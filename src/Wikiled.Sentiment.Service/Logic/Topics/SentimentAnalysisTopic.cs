using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;

namespace Wikiled.Sentiment.Service.Logic.Topics
{
    public class SentimentAnalysisTopic : ITopicProcessing
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<SentimentAnalysisTopic> logger;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IMqttServer server;

        private readonly IScheduler scheduler;

        private readonly IServiceProvider provider;

        public SentimentAnalysisTopic(
            ILogger<SentimentAnalysisTopic> logger,
            IJsonSerializer serializer,
            ILexiconLoader lexiconLoader,
            IMqttServer server,
            IScheduler scheduler,
            IServiceProvider provider)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.provider = provider;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Topic => TopicConstants.SentimentAnalysis;

        public async Task Process(MqttApplicationMessageReceivedEventArgs message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = serializer.Deserialize<WorkRequest>(message.ApplicationMessage.Payload);
            if (request?.Documents == null)
            {
                return;
            }

            if (request.Documents.Length > 500)
            {
                throw new Exception("Too many documents. Maximum is 500");
            }

            var monitor = new PerformanceMonitor(request.Documents.Length);
            using (Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(item => logger.LogInformation(monitor.ToString())))
            {
                ISentimentDataHolder loader = default;
                if (request.Dictionary != null)
                {
                    logger.LogInformation("Creating custom dictionary with {0} words", request.Dictionary.Count);
                    loader = SentimentDataHolder.Load(request.Dictionary.Select(item => new WordSentimentValueData(item.Key, new SentimentValueData(item.Value))));
                }
                else if (!string.IsNullOrEmpty(request.Domain))
                {
                    logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                    loader = lexiconLoader.GetLexicon(request.Domain);
                }

                using (var scope = provider.CreateScope())
                {
                    var container = scope.ServiceProvider.GetService<ISessionContainer>();
                    var client = container.GetTesting();
                    var converter = scope.ServiceProvider.GetService<IDocumentConverter>();
                    client.Init();
                    client.Pipeline.ResetMonitor();
                    if (loader != null)
                    {
                        client.Lexicon = loader;
                    }

                    await client.Process(request.Documents.Select(item => converter.Convert(item, request.CleanText)).ToObservable())
                                .Buffer(TimeSpan.FromSeconds(5), 10, scheduler)
                                .Select(item => ProcessResult(message.ClientId, item))
                                .Merge();
                }

                logger.LogInformation("Completed with final performance: {0}", monitor);
            }
        }

        private async Task<IList<ProcessingContext>> ProcessResult(string userId, IList<ProcessingContext> item)
        {
            var reply = new MqttApplicationMessage();
            reply.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            reply.Topic = $"Sentiment/Result/{userId}";
            await using (var memoryStream = new MemoryStream())
            {
                var stream = serializer.Serialize(item.Select(x => x.Processed).ToArray());
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                reply.Payload = memoryStream.ToArray();
                var sendResult = await server.PublishAsync(applicationMessage: reply).ConfigureAwait(false);
                logger.LogDebug("Sent: {0}", sendResult.ReasonCode);
                return item;
            }
        }
    }
}
