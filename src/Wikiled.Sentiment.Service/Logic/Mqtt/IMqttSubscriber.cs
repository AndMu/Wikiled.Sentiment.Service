namespace Wikiled.Sentiment.Service.Logic.Mqtt
{
    public interface IMqttSubscriber
    {
        IMqttSubscription CreateSubscription(MqttRequest request);
    }
}
