using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
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

        public SentimentController(ILoggerFactory factory, ITestingClient client, ILexiconLoader lexiconLoader)
            : base(factory)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.lexiconLoader = lexiconLoader ?? throw new ArgumentNullException(nameof(lexiconLoader));

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
        public async Task<Document> Parse([FromBody]SingleRequestData review)
        {
            if (review == null)
            {
                throw new ArgumentNullException(nameof(review));
            }

            if (review.Id == null)
            {
                review.Id = Guid.NewGuid().ToString();
            }


            //System.Reactive.Subjects.AsyncSubject<Document> result = client.Process(review)
            //    .Select(item => item.Processed)
            //    .FirstOrDefaultAsync().GetAwaiter();
            //await reviewSink.AddReview(review, false).ConfigureAwait(false);
            //reviewSink.Completed();
            //Document document = await result;
            //return document;
            throw new NotImplementedException();
        }
    }
}
