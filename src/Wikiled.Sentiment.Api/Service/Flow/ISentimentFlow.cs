using System;
using System.Threading;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service.Flow
{
    public interface ISentimentFlow
    {
        IObservable<Document> Start(WorkRequest request, CancellationToken token);
    }
}