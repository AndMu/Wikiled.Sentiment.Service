using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Extensions;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Controllers;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class TokenizerController : BaseController
    {
        private readonly ITextSplitter splitter;

        public TokenizerController(ILoggerFactory factory, ITextSplitter splitter)
            : base(factory)
        {
            this.splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
        }

        [Route("parse")]
        [HttpPost]
        public async Task<Document> Parse([FromBody]SingleRequestData review)
        {
            if (review == null) throw new ArgumentNullException(nameof(review));
            if (review.Id == null)
            {
                review.Id = Guid.NewGuid().ToString();
            }

            if (review.Date == null)
            {
                review.Date = DateTime.UtcNow;
            }

            var document = new Document(review.Text);
            document.Author = review.Author;
            document.Id = review.Id;
            document.DocumentTime = review.Date;
            var result = await splitter.Process(new ParseRequest(document)).ConfigureAwait(false);
            return result.Construct(null);
        }
    }
}
