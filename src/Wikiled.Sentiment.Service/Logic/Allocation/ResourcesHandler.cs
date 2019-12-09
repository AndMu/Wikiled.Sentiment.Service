using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Wikiled.Common.Utilities.Config;

namespace Wikiled.Sentiment.Service.Logic.Allocation
{
    public class ResourcesHandler : IResourcesHandler
    {
        private readonly ILogger<ResourcesHandler> logger;

        private readonly IApplicationConfiguration configuration;

        public ResourcesHandler(ILogger<ResourcesHandler> logger, IApplicationConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Allocate(string userId)
        {
            //var status = await server.GetClientStatusAsync().ConfigureAwait(false);

            //var selected = status.FirstOrDefault(item => item.ClientId == userId);

            //if (selected == null)
            //{
            //    logger.LogWarning("User <{0}> session not found. Can't train", userId);
            //    return false;
            //}

            //var now = configuration.Now;

            //if (userProcessing.TryAdd(userId, (now.AddDays(1), selected)) )
            //{
            //    return true;
            //}

            //if (userProcessing.TryGetValue(userId, out var session) &&
            //    session.Expire <= now)
            //{
            //    logger.LogWarning("Reset expired session");
            //    userProcessing[userId] = (now.AddDays(1), selected);
            //    return true;
            //}

            return false;
        }

        public void Release(string userId)
        {
            //logger.LogDebug("Release: {0}", userId);
            //userProcessing.TryRemove(userId, out var selected);
            throw new NotImplementedException();
        }
    }
}
