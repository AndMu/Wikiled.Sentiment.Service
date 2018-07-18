using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wikiled.Common.Logging;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;
using Wikiled.Sentiment.Text.Structure;
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

        private readonly TestingClient client;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IDocumentFromReviewFactory parsedFactory = new DocumentFromReviewFactory();

        public SentimentController(ILoggerFactory factory, IReviewSink reviewSink, TestingClient client, ILexiconLoader lexiconLoader)
        : base(factory)
        {
            this.reviewSink = reviewSink ?? throw new ArgumentNullException(nameof(reviewSink));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
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

            var result = client.Process(reviewSink.Reviews)
                .Select(item => item.Processed)
                .FirstOrDefaultAsync().GetAwaiter();
            reviewSink.AddReview(review, false);
            reviewSink.Completed();
            var document = await result;
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

                var data = client.Process(reviewSink.Reviews);
                var subscription = ProcessList(data, loader, monitor);

                foreach (var document in request.Documents)
                {
                    reviewSink.AddReview(document, request.CleanText);
                }

                reviewSink.Completed();
                await subscription.ConfigureAwait(false);
                subscription.Dispose();
            }

            Logger.LogInformation("Completed with final performance: {0}", monitor);
        }

        private async Task ProcessList(IObservable<ProcessingContext> data, ISentimentDataHolder loader, PerformanceMonitor monitor)
        {
            var result = data.Select(
                item =>
                {
                    if (loader != default)
                    {
                        LexiconRatingAdjustment adjustment = new LexiconRatingAdjustment(item.Review, loader);
                        adjustment.CalculateRating();
                        return parsedFactory.ReparseDocument(adjustment);
                    }

                    return item.Processed;
                });

            await result.Select(
                            item =>
                            {
                                var str = JsonConvert.SerializeObject(item);
                                var buffer = Encoding.UTF8.GetBytes(str);
                                lock (Response.Body)
                                {
                                    Response.Body.Write(buffer, 0, buffer.Length);
                                    byte[] newline = Encoding.UTF8.GetBytes(Environment.NewLine);
                                    Response.Body.Write(newline, 0, newline.Length);
                                }

                                monitor.Increment();
                                return item;
                            })
                        .LastOrDefaultAsync();
        }
    }
}
