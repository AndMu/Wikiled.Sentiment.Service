using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Logging;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Sentiment.Api.Service;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;
using Wikiled.Text.Analysis.Structure;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.Processing;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Services.Controllers
{
    public class SentimentAnalysisController : IMessageController<SentimentMessage>
    {
        private readonly ILogger<SentimentAnalysisController> logger;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IScheduler scheduler;

        private readonly IServiceProvider provider;

        private readonly IDocumentStorage storage;

        public SentimentAnalysisController(
            ILogger<SentimentAnalysisController> logger,
            ILexiconLoader lexiconLoader,
            IScheduler scheduler,
            IServiceProvider provider,
            IDocumentStorage storage)
        {
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process(IConnectionContext target, SentimentMessage message, CancellationToken token)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = message.Request;

            if (request?.Documents == null)
            {
                throw new Exception("Nothing to process");
            }

            if (request.Documents.Length > 500)
            {
                throw new Exception("Too many documents. Maximum is 500");
            }

            var completed = new CompletedMessage();
            try
            {
                var monitor = new PerformanceMonitor(request.Documents.Length);

                using (Observable.Interval(TimeSpan.FromSeconds(10))
                                 .Subscribe(item => logger.LogInformation(monitor.ToString())))
                {
                    ISentimentDataHolder lexicon = default;

                    if (request.Dictionary != null &&
                        request.Dictionary.Count > 0)
                    {
                        logger.LogInformation("Creating custom dictionary with {0} words", request.Dictionary.Count);

                        lexicon = SentimentDataHolder.Load(request.Dictionary.Select(item =>
                                                                                        new WordSentimentValueData(
                                                                                            item.Key,
                                                                                            new SentimentValueData(item.Value))));
                    }

                    if ((lexicon == null || request.AdjustDomain) &&
                       !string.IsNullOrEmpty(request.Domain))
                    {
                        logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                        var previous = lexicon;
                        lexicon = lexiconLoader.GetLexicon(request.Domain);
                        if (previous != null)
                        {
                            lexicon.Merge(previous);
                        }
                    }

                    string modelLocation = null;

                    if (!string.IsNullOrEmpty(request.Model))
                    {
                        logger.LogInformation("Using model path: {0}", request.Model);
                        modelLocation = storage.GetLocation(target.Connection.User, request.Model, ServiceConstants.Model);

                        if (!Directory.Exists(modelLocation))
                        {
                            throw new ApplicationException($"Can't find model {request.Model}");
                        }
                    }

                    using (var scope = provider.CreateScope())
                    {
                        var container = scope.ServiceProvider.GetService<ISessionContainer>();
                        container.Context.NGram = 3;
                        container.Context.ExtractAttributes = request.Emotions;

                        var client = container.GetTesting(modelLocation);
                        var converter = scope.ServiceProvider.GetService<IDocumentConverter>();
                        client.Init();
                        client.Pipeline.ResetMonitor();

                        if (lexicon != null)
                        {
                            client.Lexicon = lexicon;
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
                                        var result = new ResultMessage<Document> { Data = item.Select(x => x.Processed).ToArray() };
                                        await target.Write(result, token).ConfigureAwait(false);
                                        return Unit.Default;
                                    })
                                    .Merge();
                    }

                    logger.LogInformation("Completed with final performance: {0}", monitor);
                    completed.Message = "Testing Completed";
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
