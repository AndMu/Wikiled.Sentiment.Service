using System;
using MQTTnet;
using MQTTnet.Client.Options;

namespace Wikiled.Sentiment.Service.Logic.Mqtt
{
    public interface IMqttSubscription
    {
        IObservable<MqttApplicationMessage> Subscribe(IMqttClientOptions options, string topic);
    }
}