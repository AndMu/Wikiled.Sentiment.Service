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

        public DocumentsController(ILoggerFactory factory, IDocumentStorage storage)
            : base(factory)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        [Route("save")]
        [HttpPost]
        public async Task<ActionResult<Document>> Save([FromBody] SaveRequest save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            await storage.Save(save).ConfigureAwait(false);
            return Ok();
        }

        [Route("delete/{userId}/{name}")]
        [HttpPost]
        public ActionResult Delete(string userId, string name)
        {
            storage.Delete(userId, name);
            return Ok();
        }

        [Route("check/{userId}/{name}")]
        [HttpGet]
        public ActionResult<int> Check(string userId, string name)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var count = storage.Count(userId, name);
            return Ok(count);
        }
    }
}
