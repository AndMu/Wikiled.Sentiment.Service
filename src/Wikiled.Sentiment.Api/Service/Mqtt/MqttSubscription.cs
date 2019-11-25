using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;

namespace Wikiled.Sentiment.Api.Service.Mqtt
{
    public class MqttSubscription : IMqttApplicationMessageReceivedHandler, IMqttSubscription
    {
        private readonly ILogger<MqttSubscription> logger;

        private readonly IMqttClient client;

        private readonly Subject<MqttApplicationMessage> subscription = new Subject<MqttApplicationMessage>();

        public MqttSubscription(ILogger<MqttSubscription> logger, IMqttClient client, string topic)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            client.ApplicationMessageReceivedHandler = this;
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        }

        public string Topic { get; }

        public IObservable<MqttApplicationMessage> Subscription => subscription;

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));
            subscription.OnNext(eventArgs.ApplicationMessage);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            subscription.OnCompleted();
        }

        public async Task Start()
        {
            logger.LogDebug("Start");
            var topic = new TopicFilterBuilder()
                .WithAtMostOnceQoS()
                .WithTopic(Topic)
                .Build();

            MqttClientSubscribeResult subResult = await client
                                                        .SubscribeAsync(topic)
                                                        .ConfigureAwait(false);
            foreach (var subscribeResultItem in subResult.Items)
            {
                if (subscribeResultItem.ResultCode != MqttClientSubscribeResultCode.GrantedQoS0)
                {
                    logger.LogError("Start: {1}({0})", Topic, subscribeResultItem.ResultCode);
                    throw new Exception($"Subscription failed: {subscribeResultItem.ResultCode}");
                }

                logger.LogDebug("Start: {1}({0})", Topic, subscribeResultItem.ResultCode);
            }
        }
    }
}
