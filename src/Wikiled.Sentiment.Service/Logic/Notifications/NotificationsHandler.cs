using Microsoft.Extensions.Logging;
using Microsoft.IO;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Wikiled.Common.Utilities.Helpers;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Pipeline;

namespace Wikiled.Sentiment.Service.Logic.Notifications
{
    public class NotificationsHandler : INotificationsHandler
    {
        private readonly ILogger<NotificationsHandler> logger;

        private readonly IMqttServer server;

        private readonly IJsonSerializer serializer;

        private readonly RecyclableMemoryStreamManager memoryStreamManager;

        private readonly ObjectPool<MqttApplicationMessage> messages;

        public NotificationsHandler(ILogger<NotificationsHandler> logger, IMqttServer server, IJsonSerializer serializer, RecyclableMemoryStreamManager memoryStreamManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
            messages = ObjectPool.Create<MqttApplicationMessage>(
                new AutomaticPooledObjectPolicy<MqttApplicationMessage>(
                    () => new MqttApplicationMessage(),
                    item => true));
        }

        public async Task PublishResults(string userId, IList<ProcessingContext> item)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            await using (var memoryStream = memoryStreamManager.GetStream("Json"))
            {
                var stream = serializer.Serialize(item.Select(x => x.Processed).ToArray());
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                await Send($"Sentiment/Result/{userId}", memoryStream.ToArray()).ConfigureAwait(false);
            }
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

            return SendMessage($"{type}/{userId}", message, Encoding.ASCII);
        }

        private Task SendMessage(string topic, string message, Encoding encoding)
        {
            int minLength = encoding.GetByteCount(message);
            byte[] array = ArrayPool<byte>.Shared.Rent(minLength);
            encoding.GetBytes(message, 0, message.Length, array, 0);
            return Send(topic, array, applicationMessage => ArrayPool<byte>.Shared.Return(array));
        }

        private async Task Send(string topic, byte[] data, Action<MqttApplicationMessage> release = null)
        {
            var messageItem = messages.Get();
            try
            {
                messageItem.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
                messageItem.Topic = topic;
                messageItem.Payload = data;
                var sendResult = await server.PublishAsync(messageItem).ConfigureAwait(false);
                logger.LogDebug("Sent: {0}", sendResult.ReasonCode);
            }
            finally
            {
                release?.Invoke(messageItem);
                messages.Return(messageItem);
            }
        }
    }
}
