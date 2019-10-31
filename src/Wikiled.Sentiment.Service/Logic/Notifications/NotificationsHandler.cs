using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
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

        public NotificationsHandler(ILogger<NotificationsHandler> logger, IMqttServer server, IJsonSerializer serializer)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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

            var reply = new MqttApplicationMessage();
            reply.QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            reply.Topic = $"Sentiment/Result/{userId}";
            await using (var memoryStream = new MemoryStream())
            {
                var stream = serializer.Serialize(item.Select(x => x.Processed).ToArray());
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                reply.Payload = memoryStream.ToArray();
                var sendResult = await server.PublishAsync(applicationMessage: reply).ConfigureAwait(false);
                logger.LogDebug("Sent: {0}", sendResult.ReasonCode);
            }
        }

        public Task SendError(string userId, string message)
        {
            throw new NotImplementedException();
        }

        public Task SendMessage(string userId, string message)
        {
            throw new NotImplementedException();
        }
    }
}
