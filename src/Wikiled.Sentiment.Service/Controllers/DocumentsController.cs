using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Server.Core.Controllers;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class DocumentsController : BaseController
    {
        private readonly IDocumentStorage storage;

        public DocumentsController(ILoggerFactory factory)
            : base(factory)
        {
        }


        [Route("save")]
        [HttpPost]
        public async Task<ActionResult<Document>> Parse([FromBody]SingleRequestData[] review)
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
