using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Wikiled.MachineLearning.Mathematics;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Controllers;
using Wikiled.Text.Analysis.Extensions;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class SentimentController : BaseController
    {
        private readonly ILogger<SentimentController> logger;

        private readonly ITestingClient client;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IDocumentConverter documentConverter;

        private readonly IMemoryCache cache;

        public SentimentController(ILoggerFactory factory,
                                   ITestingClient client,
                                   ILexiconLoader lexiconLoader,
                                   IDocumentConverter documentConverter,
                                   IMemoryCache cache)
            : base(factory)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.documentConverter = documentConverter;
            this.cache = cache;
            logger = factory.CreateLogger<SentimentController>();

            client.TrackArff = false;
            client.UseBuiltInSentiment = true;

            // add limit of concurrent processing
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
        public async Task<ActionResult<Document>> Parse([FromBody]SingleWorkRequest request)
        {
            var result = await ProcessSingleRequest(request).ConfigureAwait(false);
            return Ok(result.Processed);
        }

        [Route("calculate")]
        [HttpPost]
        public async Task<ActionResult<RatingValue>> Calculate([FromBody] SingleWorkRequest request)
        {
            var result = await ProcessSingleRequest(request).ConfigureAwait(false);
            var rating = result.Review.CalculateRawRating();
            var value = new RatingValue();
            value.Stars = rating.StarsRating;
            value.Negative = rating.Negative;
            value.Positive = rating.Positive;
            return Ok(value);
        }

        private async Task<ProcessingContext> ProcessSingleRequest(SingleWorkRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Review.Id == null)
            {
                request.Review.Id = Guid.NewGuid().ToString();
            }

            if (!string.IsNullOrEmpty(request.Domain))
            {
                logger.LogInformation("Using Domain dictionary [{0}]", request.Domain);
                client.Lexicon = lexiconLoader.GetLexicon(request.Domain);
            }

            var id = GetId(request);

            var result = await cache
                               .GetOrCreateAsync(id,
                                                 async entry =>
                                                 {
                                                     entry.SlidingExpiration = TimeSpan.FromMinutes(10);

                                                     return await client.Process(
                                                                            documentConverter.Convert(request.Review, request.CleanText))
                                                                        .ConfigureAwait(false);
                                                 })
                               .ConfigureAwait(false);

            return result;
        }

        private string GetId(SingleWorkRequest request)
        {
            var textId = request.Review.Text.GenerateKey();
            return $"{request.Domain}:{request.CleanText}:{textId}";
        }
    }
}
