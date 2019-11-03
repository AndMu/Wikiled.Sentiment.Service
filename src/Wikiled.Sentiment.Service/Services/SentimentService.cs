using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wikiled.Sentiment.Service.Logic.Allocation;
using Wikiled.Sentiment.Service.Logic.Notifications;
using Wikiled.Sentiment.Service.Logic.Topics;

namespace Wikiled.Sentiment.Service.Services
{
    public class SentimentService : IMqttServerClientConnectedHandler, IMqttServerClientMessageQueueInterceptor, IMqttApplicationMessageReceivedHandler, IMqttServerClientDisconnectedHandler
    {
        private readonly ILogger<SentimentService> logger;

        private readonly ILookup<string, ITopicProcessing> topicProcessings;

        private readonly INotificationsHandler notifications;

        private readonly IResourcesHandler resourcesHandler;

        public SentimentService(ILogger<SentimentService> logger, IEnumerable<ITopicProcessing> topics, INotificationsHandler notifications, IResourcesHandler resourcesHandler)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            this.resourcesHandler = resourcesHandler ?? throw new ArgumentNullException(nameof(resourcesHandler));
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

            if (eventArgs.ClientId == null)
            {
                return;
            }

            logger.LogDebug("Handling message: {0}", eventArgs.ClientId);

            try
            {
                var allocation = await resourcesHandler.Allocate(eventArgs.ClientId).ConfigureAwait(false);
                if (!allocation)
                {
                    await notifications.SendUserMessage(eventArgs.ClientId, TopicConstants.Error, "Failed to allocate resources for training").ConfigureAwait(false);
                    return;
                }

                if (topicProcessings.Contains(eventArgs.ApplicationMessage.Topic))
                {
                    eventArgs.ProcessingFailed = true;
                    var tasks = topicProcessings[eventArgs.ApplicationMessage.Topic]
                        .Select(item => item.Process(eventArgs));
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                else
                {
                    logger.LogWarning("Unknown Topic: {0}({1})", eventArgs.ApplicationMessage.Topic, eventArgs.ClientId);
                }
            }
            catch (Exception e)
            {
                eventArgs.ProcessingFailed = true;
                await notifications.SendUserMessage(eventArgs.ClientId, TopicConstants.Error, e.Message).ConfigureAwait(false);
                logger.LogError(e, "Error");
            }
            finally
            {
                resourcesHandler.Release(eventArgs.ClientId);
                await notifications.SendUserMessage(eventArgs.ClientId, TopicConstants.SentimentAnalysisResult, $"{eventArgs.ApplicationMessage.Topic} Done").ConfigureAwait(false);
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
