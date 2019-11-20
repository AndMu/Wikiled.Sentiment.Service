using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;
using System;
using System.Threading.Tasks;
using MQTTnet.Client.Options;

namespace Wikiled.Sentiment.Service.Logic.Mqtt
{
    public class MqttSubscription : IMqttClientConnectedHandler, IMqttApplicationMessageReceivedHandler, IMqttClientDisconnectedHandler, IMqttSubscription
    {
        private readonly ILogger<MqttSubscription> logger;

        private IMqttClient client;

        public MqttSubscription(ILogger<MqttSubscription> logger, IMqttClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            client.ConnectedHandler = this;
            client.DisconnectedHandler = this;
            client.ApplicationMessageReceivedHandler = this;
        }

        public IObservable<MqttApplicationMessage> Subscribe(IMqttClientOptions options, string topic)
        {
            throw new NotImplementedException();
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
