using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Analysis.Pipeline;
using Wikiled.WebSockets.Server.Protocol.ConnectionManagement;

namespace Wikiled.Sentiment.Service.Logic.Notifications
{
    public interface INotificationsHandler
    {
        Task PublishResults(IConnectionContext connection, IList<ProcessingContext> item, CancellationToken token);

        Task SendUserMessage(IConnectionContext connection, string topic, string message);
    }
}
