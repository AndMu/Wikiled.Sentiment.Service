using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wikiled.Sentiment.Api.Service.Mqtt
{
    public interface IMqttConnection : IDisposable
    {
        Task Connect(MqttConnectionInfo connectionInfo, CancellationToken token);

        IMqttSubscription CreateSubscription(string topic);
    }
}
