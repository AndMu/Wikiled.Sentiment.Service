using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;

namespace Wikiled.Sentiment.Service.Services
{
    public class SentimentService : IMqttServerClientConnectedHandler, IMqttServerClientMessageQueueInterceptor, IMqttApplicationMessageReceivedHandler, IMqttServerClientDisconnectedHandler
    {
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
            return Task.CompletedTask;
        }

        public Task InterceptClientMessageQueueEnqueueAsync(MqttClientMessageQueueInterceptorContext context)
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
