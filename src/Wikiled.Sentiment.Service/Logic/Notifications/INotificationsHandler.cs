using System.Collections.Generic;
using System.Threading.Tasks;
using Wikiled.Sentiment.Analysis.Pipeline;

namespace Wikiled.Sentiment.Service.Logic.Notifications
{
    public interface INotificationsHandler
    {
        Task PublishResults(string userId, IList<ProcessingContext> item);

        Task SendError(string userId, string message);

        Task SendMessage(string userId, string message);
    }
}
