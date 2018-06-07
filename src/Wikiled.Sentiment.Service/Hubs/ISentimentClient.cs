using System.Threading.Tasks;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Hubs
{
    public interface ISentimentClient
    {
        Task Resolved(Document document);

        Task Ping(string id);
    }
}
