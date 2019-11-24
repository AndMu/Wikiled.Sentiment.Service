using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wikiled.Sentiment.Api.Service.Mqtt
{
    public interface IMqttConnection : IDisposable
    {
        Task Connect(MqttConnectionInfo connectionInfo, CancellationToken token);

        Task Publish(string topic, byte[] data);

        IMqttSubscription CreateSubscription(string topic);
    }
}
