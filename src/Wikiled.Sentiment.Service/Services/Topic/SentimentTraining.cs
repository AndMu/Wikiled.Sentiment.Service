using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service.Flow;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Services.Topic
{
    public class SentimentTraining : ITopicProcessing
    {
        private readonly IDocumentStorage storage;

        private readonly IJsonSerializer serializer;

        private readonly ILogger<SentimentTraining> logger;

        private readonly IServiceProvider provider;

        private readonly ILexiconLoader lexiconLoader;

        public SentimentTraining(
            ILogger<SentimentTraining> logger,
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

        public string Topic => ServiceConstants.SentimentTraining;
        public async Task Process(IConnectionContext target, SubscribeMessage message, CancellationToken token)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var request = serializer.Deserialize<TrainRequest>(message.Payload);
            ISentimentDataHolder loader = default;

            if (!string.IsNullOrEmpty(request.Domain))
            {
                logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                loader = lexiconLoader.GetLexicon(request.Domain);
            }

            var modelLocation = storage.GetLocation(target.Connection.User, request.Name, ServiceConstants.Model);

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

                var positive = storage.Load(target.Connection.User, request.Name, true)
                                       .Take(2000);
                var negative = storage.Load(target.Connection.User, request.Name, false)
                                      .Take(2000);

                var documents = positive.Concat(negative).Select(item => converter.Convert(item, request.CleanText));
                await client.Train(documents).ConfigureAwait(false);
            }
        }
    }
}
