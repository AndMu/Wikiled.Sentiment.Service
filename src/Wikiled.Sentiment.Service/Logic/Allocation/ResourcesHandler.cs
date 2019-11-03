using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Server.Status;
using Wikiled.Common.Utilities.Config;
using Wikiled.Sentiment.Service.Logic.Notifications;

namespace Wikiled.Sentiment.Service.Logic.Allocation
{
    public class ResourcesHandler : IResourcesHandler
    {
        private readonly ILogger<NotificationsHandler> logger;

        private readonly IMqttServer server;

        private readonly ConcurrentDictionary<string, (DateTime Expire, IMqttClientStatus Status)> userProcessing = new ConcurrentDictionary<string, (DateTime Expire, IMqttClientStatus Status)>();

        private readonly ConcurrentDictionary<string, bool> ipProcessing = new ConcurrentDictionary<string, bool>();

        private readonly IApplicationConfiguration configuration;

        public ResourcesHandler(ILogger<NotificationsHandler> logger, IMqttServer server, IApplicationConfiguration configuration)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Allocate(string userId)
        {
            var status = await server.GetClientStatusAsync().ConfigureAwait(false);

            var selected = status.FirstOrDefault(item => item.ClientId == userId);

            if (selected == null)
            {
                logger.LogWarning("User <{0}> session not found. Can't train", userId);
                return false;
            }

            var now = configuration.Now;
            if (userProcessing.TryAdd(userId, (now.AddDays(1), selected)))
            {
                ipProcessing[selected.Endpoint] = true;
                return true;
            }

            if (userProcessing.TryGetValue(userId, out var session) &&
                session.Expire <= now)
            {
                logger.LogWarning("Reset expired session");
                userProcessing[userId] = (now.AddDays(1), selected);
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
                logger.LogDebug("Release Endpoint: {0}", selected.Status.Endpoint);
                ipProcessing.TryRemove(selected.Status.Endpoint, out _);
            }
            
        }
    }
}
