using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Helpers;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class TokenizerController : Controller
    {
        private readonly ILogger<TokenizerController> logger;

        private readonly IIpResolve resolve;

        private readonly ITextSplitter splitter;

        public TokenizerController(ILogger<TokenizerController> logger, IIpResolve resolve, ITextSplitter splitter)
        {
            this.resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
            this.splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Route("parse")]
        [HttpPost]
        public async Task<Document> Parse([FromBody]SingleRequestData review)
        {
            logger.LogInformation("Parse [{0}]", resolve.GetRequestIp());
            if (review.Id == null)
            {
                review.Id = Guid.NewGuid().ToString();
            }

            if (review.Date == null)
            {
                review.Date = DateTime.UtcNow;
            }

            Document document = new Document(review.Text);
            document.Author = review.Author;
            document.Id = review.Id;
            document.DocumentTime = review.Date;
            var result = await splitter.Process(new ParseRequest(document));
            return result;
        }
    }
}
