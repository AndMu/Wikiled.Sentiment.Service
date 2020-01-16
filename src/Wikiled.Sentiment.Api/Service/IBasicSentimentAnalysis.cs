using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    public interface IBasicSentimentAnalysis
    {
        Task<Document> Measure(SingleWorkRequest request, CancellationToken token);

        Task<double?> Calculate(SingleWorkRequest request, CancellationToken token);
    }
}