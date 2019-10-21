using Microsoft.Extensions.Logging;
using MQTTnet;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;

namespace Wikiled.Sentiment.Service.Logic.Topics
{
    public class SentimentAnalysisTopic : ITopicProcessing
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<SentimentAnalysisTopic> logger;

        private readonly Func<ITestingClient> clientFactory;

        private readonly ILexiconLoader lexiconLoader;

        public SentimentAnalysisTopic(ILogger<SentimentAnalysisTopic> logger, IJsonSerializer serializer, Func<ITestingClient> client, ILexiconLoader lexiconLoader)
        {
            this.serializer = serializer;
            clientFactory = client;
            this.lexiconLoader = lexiconLoader;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Topic => TopicConstants.SentimentAnalysis;

        public async Task Process(MqttApplicationMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = serializer.Deserialize<WorkRequest>(message.Payload);
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

                var client = clientFactory();
                client.Init();
                if (loader != null)
                {
                    client.Lexicon = loader;
                }

                await client.Process(request.Documents.ToObservable())
                            .ForEachAsync(item => {});
            }

            logger.LogInformation("Completed with final performance: {0}", monitor);
        }
    }
}
