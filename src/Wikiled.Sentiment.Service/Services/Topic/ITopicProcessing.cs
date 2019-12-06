using System.Threading;
using System.Threading.Tasks;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Services.Topic
{
    public interface ITopicProcessing
    {
        string Topic { get; }

        Task Process(IConnectionContext target, SubscribeMessage message, CancellationToken token);
    }
}
