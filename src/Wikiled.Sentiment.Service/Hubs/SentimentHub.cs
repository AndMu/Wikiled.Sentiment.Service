using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Hubs
{
    public class SentimentHub : Hub<ISentimentClient>
    {
        public Task Resolved(Document document) => Clients.All.Resolved(document);

        public Task Ping(string id) => Clients.All.Ping(id);
    }
}
