using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Notifications;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Text.Parser;

namespace Wikiled.Sentiment.Service.Services.Topics
{
    public class SentimentTrainingTopic : ITopicProcessing
    {
        private readonly IDocumentStorage storage;

        private readonly IJsonSerializer serializer;

        private readonly ILogger<SentimentTrainingTopic> logger;

        private readonly IServiceProvider provider;

        private readonly ILexiconLoader lexiconLoader;

        public SentimentTrainingTopic(
            ILogger<SentimentTrainingTopic> logger,
            IJsonSerializer serializer,
            IDocumentStorage storage,
            ILexiconLoader lexiconLoader,
            IServiceProvider provider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public string Topic => TopicConstants.SentimentTraining;

        public async Task Process(MqttApplicationMessageReceivedEventArgs message)
        {
            if (message?.ClientId == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = serializer.Deserialize<TrainRequest>(message.ApplicationMessage.Payload);
            ISentimentDataHolder loader = default;

            if (!string.IsNullOrEmpty(request.Domain))
            {
                logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                loader = lexiconLoader.GetLexicon(request.Domain);
            }

            var modelLocation = storage.GetLocation(message.ClientId, request.Name, "model");

            using (var scope = provider.CreateScope())
            {
                var container = scope.ServiceProvider.GetService<ISessionContainer>();
                var client = container.GetTraining(modelLocation);
                var converter = scope.ServiceProvider.GetService<IDocumentConverter>();
                client.Pipeline.ResetMonitor();
                if (loader != null)
                {
                    client.Lexicon = loader;
                }

                var documents = storage.Load(message.ClientId, request.Name)
                    .Select(item => converter.Convert(item, request.CleanText));

                await client.Train(documents).ConfigureAwait(false);
            }
        }
    }
}
