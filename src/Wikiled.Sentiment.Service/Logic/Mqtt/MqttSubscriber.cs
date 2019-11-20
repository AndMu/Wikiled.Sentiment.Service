using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Server;
using Wikiled.Redis.Helpers;
using MqttClientSubscribeResult = MQTTnet.Client.Subscribing.MqttClientSubscribeResult;

namespace Wikiled.Sentiment.Service.Logic.Mqtt
{
    public class MqttSubscriber : IMqttSubscriber
    {
        private readonly ILogger<MqttSubscriber> logger;

        private readonly IMqttClientFactory factory;

        public MqttSubscriber(ILogger<MqttSubscriber> logger, IMqttClientFactory factory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IMqttSubscription CreateSubscription(MqttRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return Observable.Create<MqttApplicationMessage>(async (observer, token) =>
            {
                try
                {
                    await MqttProcessing(request, observer, token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed processing");
                    observer.OnError(e);
                }
            });
        }

        private async Task MqttProcessing(MqttRequest request, IObserver<MqttApplicationMessage> observer, CancellationToken token)
        {
            var stopSource = new CancellationTokenSource();
            token.Register(stopSource.Cancel);
            var uri = request.Uri.ToString();
            logger.LogInformation("Subscribing: {0}", uri);
            var options = new MqttClientOptionsBuilder()
                          .WithWebSocketServer(uri)
                          .WithProtocolVersion(MqttProtocolVersion.V310)
                          .WithClientId(request.ClientId)
                          .Build();

            var mqttClient = factory.CreateMqttClient();
            var authResult = await mqttClient.ConnectAsync(options, token).ConfigureAwait(false);
            logger.LogDebug("Mqtt connection result: {0}", authResult.ResultCode);

            if (authResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new ConnectionException($"Connection failed: {authResult}");
            }

            try
            {
                var topic = new TopicFilterBuilder().WithTopic(request.Topic).Build();

                MqttClientSubscribeResult subResult = await mqttClient
                                                            .SubscribeAsync(topic)
                                                            .ConfigureAwait(false);

                mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(e =>
                {
                    logger.LogInformation("Disconnection detected");
                    if (e.Exception != null)
                    {
                        logger.LogError(e.Exception, "Disconnection");
                    }

                    observer.OnCompleted();
                    stopSource.Cancel();
                });

                mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    logger.LogDebug("Message received");
                    observer.OnNext(e.ApplicationMessage);
                });

                await stopSource.Token.AsTask().ConfigureAwait(false);
                observer.OnCompleted();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed");
                observer.OnError(e);
            }
            finally
            {
                logger.LogInformation("Cleaning connection...");
                mqttClient.DisconnectedHandler = null;
                mqttClient.ApplicationMessageReceivedHandler = null;

                if (mqttClient.IsConnected)
                {
                    logger.LogInformation("Closing connection...");
                    using var disconnectTokenSource = new CancellationTokenSource(500);
                    await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), disconnectTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        public Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
