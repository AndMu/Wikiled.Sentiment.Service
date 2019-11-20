using System;

namespace Wikiled.Sentiment.Service.Logic.Mqtt
{
    public class MqttRequest
    {
        public MqttRequest(Uri uri, string clientId, string topic)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        }

        public Uri Uri { get; }

        public string ClientId { get; }

        public string Topic { get; }
    }
}
