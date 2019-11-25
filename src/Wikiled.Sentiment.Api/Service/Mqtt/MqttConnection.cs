using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;

namespace Wikiled.Sentiment.Api.Service.Mqtt
{
    public class MqttConnection : IMqttConnection, IMqttClientConnectedHandler, IMqttClientDisconnectedHandler
    {
        private readonly ILogger<MqttConnection> logger;

        private readonly ILoggerFactory loggerFactory;

        private IMqttClient client;

        private ConcurrentBag<IMqttSubscription> bag = new ConcurrentBag<IMqttSubscription>();

        private bool connected;

        public MqttConnection(ILoggerFactory loggerFactory, IMqttClientFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            logger = loggerFactory.CreateLogger<MqttConnection>();
            client = factory.CreateMqttClient();
        }
      
        public async Task Connect(MqttConnectionInfo connectionInfo, CancellationToken token)
        {
            if (connectionInfo == null) throw new ArgumentNullException(nameof(connectionInfo));

            if (connected)
            {
                throw new Exception("Already connected");
            }

            var options = new MqttClientOptionsBuilder()
                          .WithWebSocketServer(connectionInfo.Uri.ToString())
                          .WithProtocolVersion(MqttProtocolVersion.V310)
                          .WithClientId(connectionInfo.ClientId)
                          .Build();
            
            var authResult = await client.ConnectAsync(options, token).ConfigureAwait(false);
            logger.LogDebug("Mqtt connection result: {0}", authResult.ResultCode);
            if (authResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new Exception($"Connection failed: {authResult}");
            }

            connected = true;
        }

        public Task Publish(string topic, byte[] data)
        {
            if (!connected)
            {
                throw new Exception("Not connected yet");
            }

            return client.PublishAsync(topic, data);

        }

        public IMqttSubscription CreateSubscription(string topic)
        {
            var subscription = new MqttSubscription(loggerFactory.CreateLogger<MqttSubscription>(), client, topic);
            bag.Add(subscription);
            return subscription;
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            logger.LogDebug("HandleDisconnectedAsync");
            return Task.CompletedTask;
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            logger.LogDebug("HandleDisconnectedAsync");
            ReleaseConnection();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            logger.LogDebug("Dispose");
            ReleaseConnection();
            client?.Dispose();
        }

        private void ReleaseConnection()
        {
            logger.LogDebug("ReleaseConnection");
            foreach (var subscription in bag)
            {
                subscription.Dispose();
            }

            bag = new ConcurrentBag<IMqttSubscription>();
            client.Dispose();
            client = null;
        }
        
    }
}
