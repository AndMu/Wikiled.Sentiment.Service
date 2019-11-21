using System;
using System.Threading.Tasks;
using MQTTnet;

namespace Wikiled.Sentiment.Api.Service.Mqtt
{
    public interface IMqttSubscription : IDisposable
    {
        string Topic { get; }

        IObservable<MqttApplicationMessage> Subscription { get; }

        Task Start();
    }
}