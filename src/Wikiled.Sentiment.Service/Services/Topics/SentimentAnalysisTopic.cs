using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Notifications;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;

namespace Wikiled.Sentiment.Service.Services.Topics
{
    public class SentimentAnalysisTopic : ITopicProcessing
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<SentimentAnalysisTopic> logger;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IScheduler scheduler;

        private readonly IServiceProvider provider;

        private readonly INotificationsHandler notifications;

        private readonly IDocumentStorage storage;

        public SentimentAnalysisTopic(
            ILogger<SentimentAnalysisTopic> logger,
            IJsonSerializer serializer,
            ILexiconLoader lexiconLoader,
            IScheduler scheduler,
            IServiceProvider provider,
            INotificationsHandler notifications, 
            IDocumentStorage storage)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
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
            using (Observable.Interval(TimeSpan.FromSeconds(10))
                .Subscribe(item => logger.LogInformation(monitor.ToString())))
            {
                ISentimentDataHolder loader = default;
                if (request.Dictionary != null)
                {
                    logger.LogInformation("Creating custom dictionary with {0} words", request.Dictionary.Count);
                    loader = SentimentDataHolder.Load(request.Dictionary.Select(item =>
                        new WordSentimentValueData(item.Key, new SentimentValueData(item.Value))));
                }
                else if (!string.IsNullOrEmpty(request.Domain))
                {
                    logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                    loader = lexiconLoader.GetLexicon(request.Domain);
                }

                string modelLocation = null;
                if (!string.IsNullOrEmpty(request.Model))
                {
                    logger.LogInformation("Using model path: {0}", request.Model);
                    modelLocation = storage.GetLocation(message.ClientId, request.Model, TopicConstants.Model);
                    if (!Directory.Exists(modelLocation))
                    {
                        throw new ApplicationException($"Can't find model {request.Model}");
                    }
                }

                using (var scope = provider.CreateScope())
                {
                    var container = scope.ServiceProvider.GetService<ISessionContainer>();

                    var client = container.GetTesting(modelLocation);
                    var converter = scope.ServiceProvider.GetService<IDocumentConverter>();
                    client.Init();
                    client.Pipeline.ResetMonitor();
                    if (loader != null)
                    {
                        client.Lexicon = loader;
                    }

                    await client.Process(request.Documents.Select(item => converter.Convert(item, request.CleanText))
                            .ToObservable())
                        .Select(item =>
                        {
                            monitor.Increment();
                            return item;
                        })
                        .Buffer(TimeSpan.FromSeconds(5), 10, scheduler)
                        .Select(async item =>
                        {
                            await notifications.PublishResults(message.ClientId, item).ConfigureAwait(false);
                            return Unit.Default;
                        })
                        .Merge();
                }

                logger.LogInformation("Completed with final performance: {0}", monitor);
            }
        }
    }
}
