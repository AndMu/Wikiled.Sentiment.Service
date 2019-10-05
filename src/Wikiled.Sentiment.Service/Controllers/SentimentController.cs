using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Logging;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Controllers;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class SentimentController : BaseController
    {
        private readonly IReviewSink reviewSink;

        private readonly ITestingClient client;

        private readonly ILexiconLoader lexiconLoader;

        private readonly SemaphoreSlim syncRoot = new SemaphoreSlim(1);

        public SentimentController(ILoggerFactory factory, IReviewSink reviewSink, ITestingClient client, ILexiconLoader lexiconLoader)
            : base(factory)
        {
            this.reviewSink = reviewSink ?? throw new ArgumentNullException(nameof(reviewSink));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));

            client.TrackArff = false;
            client.UseBuiltInSentiment = true;
            // add limit of concurrent processing
            reviewSink.ProcessingSemaphore = new SemaphoreSlim(200);
            client.Init();
        }

        [Route("domains")]
        [HttpGet]
        public string[] SupportedDomains()
        {
            return lexiconLoader.Supported.ToArray();
        }

        [Route("parse")]
        [HttpPost]
        public async Task<Document> Parse([FromBody]SingleRequestData review)
        {
            if (review.Id == null)
            {
                review.Id = Guid.NewGuid().ToString();
            }

            System.Reactive.Subjects.AsyncSubject<Document> result = client.Process(reviewSink.Reviews)
                .Select(item => item.Processed)
                .FirstOrDefaultAsync().GetAwaiter();
            await reviewSink.AddReview(review, false).ConfigureAwait(false);
            reviewSink.Completed();
            Document document = await result;
            return document;
        }

        [HttpPost]
        [Route("parsestream")]
        public async Task GetStream([FromBody] WorkRequest request)
        {
            Response.ContentType = "application/json";
            if (request?.Documents == null)
            {
                return;
            }

            if (request.Documents.Length > 500)
            {
                throw new Exception("Too many documents. Maximum is 500");
            }

            var monitor = new PerformanceMonitor(request.Documents.Length);
            using (Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(item => Logger.LogInformation(monitor.ToString())))
            {
                ISentimentDataHolder loader = default;
                if (request.Dictionary != null)
                {
                    Logger.LogInformation("Creating custom dictionary with {0} words", request.Dictionary.Count);
                    loader = SentimentDataHolder.Load(request.Dictionary.Select(item => new WordSentimentValueData(item.Key, new SentimentValueData(item.Value))));
                }
                else if (!string.IsNullOrEmpty(request.Domain))
                {
                    Logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                    loader = lexiconLoader.GetLexicon(request.Domain);
                }

                if (loader != null)
                {
                    client.Lexicon = loader;
                }

                IObservable<ProcessingContext> data = client.Process(reviewSink.Reviews);
                Task subscription = ProcessList(data, monitor);

                foreach (SingleRequestData document in request.Documents)
                {
                    await reviewSink.AddReview(document, request.CleanText).ConfigureAwait(false);
                }

                reviewSink.Completed();
                await subscription.ConfigureAwait(false);
                subscription.Dispose();
            }

            Logger.LogInformation("Completed with final performance: {0}", monitor);
        }

        private async Task ProcessList(IObservable<ProcessingContext> data, PerformanceMonitor monitor)
        {
            await data.Select(item => SendItem(monitor, item))
                      .Merge()
                      .LastOrDefaultAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private async Task<Document> SendItem(PerformanceMonitor monitor, ProcessingContext item)
        {
            try
            {
                var str = JsonConvert.SerializeObject(item.Processed);
                var buffer = Encoding.UTF8.GetBytes(str);
                using (var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                {
                    await syncRoot.WaitAsync(tokenSource.Token).ConfigureAwait(false);
                    await Response.Body.WriteAsync(buffer, 0, buffer.Length, tokenSource.Token).ConfigureAwait(false);
                    var newline = Encoding.UTF8.GetBytes(Environment.NewLine);
                    await Response.Body.WriteAsync(newline, 0, newline.Length, tokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed");
                throw;
            }
            finally
            {
                reviewSink.ProcessingSemaphore?.Release();
                syncRoot.Release();
                monitor.Increment();
            }

            return item.Processed;
        }
    }
}
