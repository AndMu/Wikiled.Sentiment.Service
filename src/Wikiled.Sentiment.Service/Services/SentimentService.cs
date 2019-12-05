using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.Context;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Services
{
    public class SentimentService : IContextSubscriber
    {
        private readonly ILogger<SentimentService> logger;

        public SentimentService(ILogger<SentimentService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string SubscriptionType => "SentimentAnalysis";

        //private readonly ILookup<string, ITopicProcessing> topicProcessings;

        //private readonly INotificationsHandler notifications;

        //private readonly IResourcesHandler resourcesHandler;

        //public SentimentService(ILogger<SentimentService> logger, IEnumerable<ITopicProcessing> topics, INotificationsHandler notifications, IResourcesHandler resourcesHandler)
        //{
        //    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        //    this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        //    this.resourcesHandler = resourcesHandler ?? throw new ArgumentNullException(nameof(resourcesHandler));
        //    topicProcessings = topics.ToLookup(item => item.Topic, item => item, StringComparer.OrdinalIgnoreCase);
        //}

        //public void ConfigureMqttServer(IMqttServer server)
        //{
        //    if (server == null)
        //    {
        //        throw new ArgumentNullException(nameof(server));
        //    }

        //    server.ClientConnectedHandler = this;
        //    server.ApplicationMessageReceivedHandler = this;
        //}


        //public Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs eventArgs)
        //{
        //    if (eventArgs == null)
        //    {
        //        throw new ArgumentNullException(nameof(eventArgs));
        //    }

        //    logger.LogDebug("Connected: {0}", eventArgs.ClientId);
        //    return Task.CompletedTask;
        //}

        //public Task InterceptClientMessageQueueEnqueueAsync(MqttClientMessageQueueInterceptorContext context)
        //{
        //    return Task.CompletedTask;
        //}

        //public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        //{
        //    if (eventArgs == null)
        //    {
        //        throw new ArgumentNullException(nameof(eventArgs));
        //    }

        //    if (eventArgs.ClientId == null)
        //    {
        //        return Task.CompletedTask;
        //    }

        //    logger.LogDebug("Handling message: {0}", eventArgs.ClientId);
        //    Task.Run(() => Processing(eventArgs)).ForgetOrThrow(logger);
        //    return Task.CompletedTask;
        //}

        //public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        //{
        //    if (eventArgs == null)
        //    {
        //        throw new ArgumentNullException(nameof(eventArgs));
        //    }

        //    logger.LogDebug("Disconnected: {0}", eventArgs.ClientId);
        //    return Task.CompletedTask;
        //}

        //private async Task Processing(MqttApplicationMessageReceivedEventArgs eventArgs)
        //{
        //    try
        //    {
        //        var allocation = await resourcesHandler.Allocate(eventArgs.ClientId).ConfigureAwait(false);
        //        if (!allocation)
        //        {
        //            eventArgs.ProcessingFailed = true;
        //            await notifications
        //                .SendUserMessage(eventArgs.ClientId, TopicConstants.Error, "Failed to allocate resources for training")
        //                .ConfigureAwait(false);
        //            return;
        //        }

        //        if (topicProcessings.Contains(eventArgs.ApplicationMessage.Topic))
        //        {
        //            var tasks = topicProcessings[eventArgs.ApplicationMessage.Topic]
        //                .Select(item => item.Process(eventArgs));
        //            await Task.WhenAll(tasks).ConfigureAwait(false);
        //            await notifications.SendUserMessage(eventArgs.ClientId, TopicConstants.SentimentDone, "Done")
        //                .ConfigureAwait(false);
        //        }
        //        else
        //        {
        //            logger.LogWarning("Unknown Topic: {0}({1})", eventArgs.ApplicationMessage.Topic, eventArgs.ClientId);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        eventArgs.ProcessingFailed = true;
        //        await notifications.SendUserMessage(eventArgs.ClientId, TopicConstants.Error, e.Message).ConfigureAwait(false);
        //        logger.LogError(e, "Error");
        //    }
        //    finally
        //    {
        //        resourcesHandler.Release(eventArgs.ClientId);
        //        await notifications
        //            .SendUserMessage(eventArgs.ClientId, TopicConstants.Message, $"{eventArgs.ApplicationMessage.Topic} Done")
        //            .ConfigureAwait(false);
        //    }
        //}

        public Task<IContextSubscription> Subscribe(IConnectionContext target, SubscribeMessage message, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
