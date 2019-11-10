using Microsoft.Extensions.Logging;
using Microsoft.IO;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.Sentiment.Service.Logic.Topics;

namespace Wikiled.Sentiment.Service.Logic.Notifications
{
    public class NotificationsHandler : INotificationsHandler
    {
        private readonly ILogger<NotificationsHandler> logger;

        private readonly IMqttServer server;

        private readonly IJsonSerializer serializer;

        private readonly RecyclableMemoryStreamManager memoryStreamManager;

        public NotificationsHandler(ILogger<NotificationsHandler> logger, IMqttServer server, IJsonSerializer serializer, RecyclableMemoryStreamManager memoryStreamManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));
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
                await Send($"{TopicConstants.SentimentAnalysisResult}/{userId}", memoryStream.ToArray()).ConfigureAwait(false);
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

            return SendMessage($"{type}/{userId}", message, Encoding.UTF8);
        }

        private Task SendMessage(string topic, string message, Encoding encoding)
        {
            byte[] array = encoding.GetBytes(message);
            return Send(topic, array);
        }

        private async Task Send(string topic, byte[] data)
        {
            var messageItem = new MqttApplicationMessage();
            messageItem.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            messageItem.PayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData;
            messageItem.Topic = topic;
            messageItem.Payload = data;
            var sendResult = await server.PublishAsync(messageItem).ConfigureAwait(false);
            logger.LogDebug("Sent: {0}", sendResult.ReasonCode);
        }
    }
}
