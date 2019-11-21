using System;

namespace Wikiled.Sentiment.Api.Service.Mqtt
{
    public class MqttConnectionInfo
    {
        public MqttConnectionInfo(Uri uri, string clientId)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public Uri Uri { get; }

        public string ClientId { get; }
    }
}
