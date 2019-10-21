using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using System;
using System.Linq;
using System.Threading.Tasks;
using Wikiled.Sentiment.Service.Logic.Topics;

namespace Wikiled.Sentiment.Service.Services
{
    public class SentimentService : IMqttServerClientConnectedHandler, IMqttServerClientMessageQueueInterceptor, IMqttApplicationMessageReceivedHandler, IMqttServerClientDisconnectedHandler
    {
        private readonly ILogger<SentimentService> logger;

        private readonly ILookup<string, ITopicProcessing> topicProcessings;

        public SentimentService(ILogger<SentimentService> logger, ITopicProcessing[] topics)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            topicProcessings = topics.ToLookup(item => item.Topic, item => item, StringComparer.OrdinalIgnoreCase);
        }

        public void ConfigureMqttServer(IMqttServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            server.ClientConnectedHandler = this;
            server.ApplicationMessageReceivedHandler = this;
        }


        public Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            logger.LogDebug("Connected: {0}", eventArgs.ClientId);
            return Task.CompletedTask;
        }

        public Task InterceptClientMessageQueueEnqueueAsync(MqttClientMessageQueueInterceptorContext context)
        {
            return Task.CompletedTask;
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            logger.LogDebug("Handling message: {0}", eventArgs.ClientId);

            if (eventArgs.ProcessingFailed)
            {
                logger.LogWarning("Processing message failed: {0}", eventArgs.ClientId);
                return;
            }

            if (topicProcessings.Contains(eventArgs.ApplicationMessage.Topic))
            {
                var tasks = topicProcessings[eventArgs.ApplicationMessage.Topic]
                            .Select(item => item.Process(eventArgs.ApplicationMessage));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                logger.LogWarning("Unknown Topic: {0}({1})", eventArgs.ApplicationMessage.Topic, eventArgs.ClientId);
            }
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            logger.LogDebug("Disconnected: {0}", eventArgs.ClientId);
            return Task.CompletedTask;
        }
    }
}
