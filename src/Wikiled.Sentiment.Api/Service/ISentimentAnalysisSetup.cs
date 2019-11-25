using Wikiled.Sentiment.Api.Request;

namespace Wikiled.Sentiment.Api.Service
{
    public interface ISentimentAnalysisSetup
    {
        ISentimentAnalysis Setup(WorkRequest request);
    }
}