using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Server.Status;
using Wikiled.Sentiment.Service.Logic.Notifications;

namespace Wikiled.Sentiment.Service.Logic.Allocation
{
    public class ResourcesHandler : IResourcesHandler
    {
        private readonly ILogger<NotificationsHandler> logger;

        private readonly IMqttServer server;

        private readonly ConcurrentDictionary<string, IMqttClientStatus> userProcessing = new ConcurrentDictionary<string, IMqttClientStatus>();

        private readonly ConcurrentDictionary<string, bool> ipProcessing = new ConcurrentDictionary<string, bool>();

        public ResourcesHandler(ILogger<NotificationsHandler> logger, IMqttServer server)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> AllocateTraining(string userId)
        {
            var status = await server.GetClientStatusAsync().ConfigureAwait(false);

            var selected = status.FirstOrDefault(item => item.ClientId == userId);

            if (selected == null)
            {
                logger.LogWarning("User <{0}> session not found. Can't train", userId);
                return false;
            }

            if (userProcessing.TryAdd(userId, selected))
            {
                ipProcessing[selected.Endpoint] = true;
                return true;
            }

            return false;
        }

        public void Release(string userId)
        {
            logger.LogDebug("Release: {0}", userId);
            if (userProcessing.TryRemove(userId, out var selected))
            {
                logger.LogDebug("Release Endpoint: {0}", selected.Endpoint);
                ipProcessing.TryRemove(selected.Endpoint, out _);
            }
            
        }
    }
}
