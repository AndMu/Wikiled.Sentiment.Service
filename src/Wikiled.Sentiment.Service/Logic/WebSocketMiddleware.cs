using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Sentiment.Service.Services.Controllers;
using Wikiled.WebSockets.Server.Processing;
using Wikiled.WebSockets.Server.Protocol;

namespace Wikiled.Sentiment.Service.Logic
{
    public class WebSocketMiddleware2
    {
        private readonly RequestDelegate next;

        private readonly ILogger<WebSocketMiddleware2> logger;

        private readonly IServiceProvider serviceProvider;

        public WebSocketMiddleware2(ILogger<WebSocketMiddleware2> logger, RequestDelegate next, IServiceProvider serviceScopeFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            serviceProvider = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    await next.Invoke(context).ConfigureAwait(false);
                }

                // ask the protocol to accept the connection, the websocket upgrade request will only be accepted
                // if the pre-conditions are satisfied (for example, the max connections has not been exceeded, etc)
                using (var scope = serviceProvider.CreateScope())
                {
                    await scope.ServiceProvider.GetService<IMessagePipeline>().StartAsync(context).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected exception occurred, the websocket will be closed");

                throw;
            }
        }
    }
}
