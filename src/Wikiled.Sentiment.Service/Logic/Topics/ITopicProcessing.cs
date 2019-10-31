using System.Threading.Tasks;
using MQTTnet;

namespace Wikiled.Sentiment.Service.Logic.Topics
{
    public interface ITopicProcessing
    {
        string Topic { get; }

        Task Process(MqttApplicationMessageReceivedEventArgs message);
    }
}