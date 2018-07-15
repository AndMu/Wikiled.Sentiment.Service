using System;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    public interface ISentimentAnalysis
    {
        Task<Document> Measure(SingleRequestData document, CancellationToken token);
        IObservable<Document> Measure(SingleRequestData[] documents, CancellationToken token);
        Task<Document> Measure(string text, CancellationToken token);
    }
}