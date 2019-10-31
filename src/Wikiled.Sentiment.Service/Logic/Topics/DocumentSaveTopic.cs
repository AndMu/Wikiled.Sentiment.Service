using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Service.Logic.Storage;

namespace Wikiled.Sentiment.Service.Logic.Topics
{
    public class DocumentSaveTopic : ITopicProcessing
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<DocumentSaveTopic> logger;

        private readonly IDocumentStorage storage;

        public DocumentSaveTopic(ILogger<DocumentSaveTopic> logger, IJsonSerializer serializer, IDocumentStorage storage)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public string Topic => TopicConstants.SentimentSave;

        public async Task Process(MqttApplicationMessageReceivedEventArgs message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var request = serializer.Deserialize<SaveRequest>(message.ApplicationMessage.Payload);
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

            await storage.Save(message.ClientId, request).ConfigureAwait(false);
        }
    }
}
