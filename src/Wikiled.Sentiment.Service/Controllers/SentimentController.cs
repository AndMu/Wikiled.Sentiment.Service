using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Request;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;
using Wikiled.Sentiment.Text.Structure;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Helpers;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class SentimentController : Controller
    {
        private readonly ILogger<SentimentController> logger;

        private readonly IReviewSink reviewSink;

        private TestingClient client;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IIpResolve resolve;

        private readonly IDocumentFromReviewFactory parsedFactory = new DocumentFromReviewFactory();

        public SentimentController(ILogger<SentimentController> logger, IReviewSink reviewSink, TestingClient client, IIpResolve resolve, ILexiconLoader lexiconLoader)
        {
            Guard.NotNull(() => reviewSink, reviewSink);
            Guard.NotNull(() => client, client);
            Guard.NotNull(() => resolve, resolve);
            Guard.NotNull(() => lexiconLoader, lexiconLoader);

            this.reviewSink = reviewSink;
            this.client = client;
            this.resolve = resolve;
            this.lexiconLoader = lexiconLoader;
            this.logger = logger;
        }

        [Route("domains")]
        [HttpGet]
        public string[] SupportedDomains()
        {
            return lexiconLoader.Supported.ToArray();
        }

        [Route("parse")]
        [HttpPost]
        public async Task<Document> Parse([FromBody]SingleProcessingData review)
        {
            logger.LogInformation("Parse [{0}]", resolve.GetRequestIp());
            review.Id = Guid.NewGuid().ToString();
            var result = reviewSink.ParsedReviews
                .Select(item => item.Processed).Where(item => item.Id == review.Id)
                .FirstOrDefaultAsync().GetAwaiter();
            reviewSink.AddReview(review, false);
            var document = await result;
            return document;
        }

        [HttpPost]
        [Route("parsestream")]
        public async Task GetStream([FromBody] WorkRequest request)
        {
            logger.LogInformation("GetStream [{0}] with <{1}> documents", resolve.GetRequestIp(), request?.Documents?.Length);
            Response.ContentType = "application/json";
            try
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

                Dictionary<string, SingleProcessingData> documentTable = new Dictionary<string, SingleProcessingData>();

                foreach (var document in request.Documents)
                {
                    if (document.Id == null ||
                        documentTable.ContainsKey(document.Id))
                    {
                        document.Id = Guid.NewGuid().ToString();
                    }

                    documentTable[document.Id] = document;
                }

                var data = reviewSink.ParsedReviews.Where(item => documentTable.ContainsKey(item.Processed.Id));
                AsyncCountdownEvent count = new AsyncCountdownEvent(request.Documents.Length);
                var task = ProcessList(data, loader, count);

                foreach (var document in request.Documents)
                {
                    reviewSink.AddReview(document, request.CleanText);
                }

                await Task.WhenAny(task, count.WaitAsync());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed");
            }
        }

        private async Task ProcessList(IObservable<ProcessingContext> data, ISentimentDataHolder loader, AsyncCountdownEvent count)
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

                                count.Signal();
                                return item;
                            })
                        .LastOrDefaultAsync();
        }

        [Route("version")]
        [HttpGet]

        public string ServerVersion()
        {
            var version = $"Version: [{Assembly.GetExecutingAssembly().GetName().Version}]";
            logger.LogInformation("Version request: {0}", version);
            return version;
        }
    }
}
