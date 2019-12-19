using System;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    public interface ISentimentAnalysis : IDisposable
    {
        Task Connect(Uri uri);

        Task<Document> Measure(SingleRequestData document, CancellationToken token);
        
		IObservable<Document> Measure(SingleRequestData[] documents, CancellationToken token);
		
        Task<Document> Measure(string text, CancellationToken token);

        Task<double?> Measure(string text);

        IObservable<(string, double?)> Measure((string Id, string Text)[] items, CancellationToken token);

        WorkRequest Settings { get; }
    }
}