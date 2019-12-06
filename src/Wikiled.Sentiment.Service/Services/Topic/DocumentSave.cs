using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service.Flow;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Services.Topic
{
    public class DocumentSave : ITopicProcessing
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<DocumentSave> logger;

        private readonly IDocumentStorage storage;

        public DocumentSave(ILogger<DocumentSave> logger, IJsonSerializer serializer, IDocumentStorage storage)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public string Topic => ServiceConstants.DocumentSave;

        public async Task Process(IConnectionContext target, SubscribeMessage message, CancellationToken token)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = serializer.Deserialize<SaveRequest>(message.Payload);
            if (request?.Documents == null)
            {
                return;
            }

            if (request.Documents.Length > 500)
            {
                throw new Exception("Too many documents. Maximum is 500");
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                throw new Exception("Name not specified");
            }

            await storage.Save(request).ConfigureAwait(false);
        }
    }
}
