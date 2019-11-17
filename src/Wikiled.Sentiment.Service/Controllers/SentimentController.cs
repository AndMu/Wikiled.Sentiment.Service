using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Controllers;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class SentimentController : BaseController
    {
        private readonly ITestingClient client;

        private readonly ILexiconLoader lexiconLoader;

        private readonly IDocumentConverter documentConverter;

        public SentimentController(ILoggerFactory factory, ITestingClient client, ILexiconLoader lexiconLoader, IDocumentConverter documentConverter)
            : base(factory)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));
            this.documentConverter = documentConverter;

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
        public async Task<ActionResult<Document>> Parse([FromBody]SingleRequestData review)
        {
            if (review == null)
            {
                throw new ArgumentNullException(nameof(review));
            }

            if (review.Id == null)
            {
                review.Id = Guid.NewGuid().ToString();
            }

            var result = await client.Process(documentConverter.Convert(review, true)).ConfigureAwait(false);
            return Ok(result.Processed);
        }
    }
}
