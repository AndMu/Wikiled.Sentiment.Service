using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.Sentiment.Api.Service.Flow;
using Wikiled.Text.Analysis.Structure;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Logic.Notifications
{
    public class NotificationsHandler : INotificationsHandler
    {
        private readonly ILogger<NotificationsHandler> logger;

        private readonly IJsonSerializer serializer;

        private readonly RecyclableMemoryStreamManager memoryStreamManager;

        public NotificationsHandler(ILogger<NotificationsHandler> logger, IJsonSerializer serializer, RecyclableMemoryStreamManager memoryStreamManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
        }

        public async Task PublishResults(IConnectionContext connection, IList<ProcessingContext> item, CancellationToken token)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (item == null) throw new ArgumentNullException(nameof(item));

            var result = new ResultMessage<Document> { Data = item.Select(x => x.Processed).ToArray() };
            await connection.Write(result, token);
        }

        public Task SendUserMessage(string userId, string type, string message)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return SendMessage($"{type}/{userId}", message, Encoding.UTF8);
        }

        private Task SendMessage(string topic, string message, Encoding encoding)
        {
            byte[] array = encoding.GetBytes(message);
            return Send(topic, array);
        }

        private async Task Send(string topic, byte[] data)
        {
            logger.LogDebug("Send: {0}", topic);
            var messageItem = new MqttApplicationMessage();
            messageItem.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            messageItem.PayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData;
            messageItem.Topic = topic;
            messageItem.Payload = data;
            var sendResult = await server.PublishAsync(messageItem).ConfigureAwait(false);
            logger.LogDebug("Sent: {0}", sendResult.ReasonCode);
        }
    }
}
