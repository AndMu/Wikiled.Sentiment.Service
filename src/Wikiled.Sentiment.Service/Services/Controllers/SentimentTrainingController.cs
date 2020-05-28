using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Sentiment.Api.Service;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.WebSockets.Server.Processing;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Services.Controllers
{
    public class SentimentTrainingController : IMessageController<TrainMessage>
    {
        private readonly IDocumentStorage storage;

        private readonly ILogger<SentimentTrainingController> logger;

        private readonly IServiceProvider provider;

        private readonly ILexiconLoader lexiconLoader;

        public SentimentTrainingController(
            ILogger<SentimentTrainingController> logger,
            IDocumentStorage storage,
            ILexiconLoader lexiconLoader,
            IServiceProvider provider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task Process(IConnectionContext target, TrainMessage request, CancellationToken token)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (request == null) throw new ArgumentNullException(nameof(request));

            ISentimentDataHolder loader = default;
            var completed = new CompletedMessage();
            try
            {
                if (!string.IsNullOrEmpty(request.Domain))
                {
                    logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                    loader = lexiconLoader.GetLexicon(request.Domain);
                }

                var modelLocation = storage.GetLocation(target.Connection.User, request.Name, ServiceConstants.Model);

                using (var scope = provider.CreateScope())
                {
                    var container = scope.ServiceProvider.GetService<ISessionContainer>();
                    container.Context.NGram = 3;
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

                    var documents = positive.Concat(negative)
                        .Select(item => converter.Convert(item, request.CleanText));
                    await client.Train(documents).ConfigureAwait(false);
                    
                    completed.Message = "Training Completed";
                    await target.Write(completed, token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                completed.Message = e.Message;
                await target.Write(completed, token).ConfigureAwait(false);
                completed.IsError = true;
                throw;
            }
        }
    }
}
