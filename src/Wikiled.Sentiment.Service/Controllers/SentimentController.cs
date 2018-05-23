using System;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    public class SentimentController : Controller
    {
        private readonly ILogger<SentimentController> logger;

        private readonly IReviewSink reviewSink;

        private TestingClient client;

        public SentimentController(ILogger<SentimentController> logger, IReviewSink reviewSink, TestingClient client)
        {
            Guard.NotNull(() => reviewSink, reviewSink);
            this.reviewSink = reviewSink;
            this.client = client;
            this.logger = logger;
        }

        [Route("Parse")]
        [HttpPost]
        public async Task<Document> Parse([FromBody]SingleProcessingData review)
        {
            review.Id = Guid.NewGuid().ToString();
            var result = reviewSink.ParsedReviews.Select(item => item.Processed).Where(item => item.Id == review.Id).FirstOrDefaultAsync();
            reviewSink.AddReview(review);
            return await result;
        }

        [Route("version")]
        [HttpGet]

        public string ServerVersion()
        {
            var version = $"Version: [{Assembly.GetExecutingAssembly().GetName().Version}]]";
            logger.LogInformation("Version request: {0}", version);
            return version;
        }

    }
}
