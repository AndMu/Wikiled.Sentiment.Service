﻿using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    public class NullSentimentAnalysis : ISentimentAnalysis
    {
        public Task<Document> Measure(SingleRequestData document, CancellationToken token)
        {
            return Task.FromResult((Document)null);
        }

        public IObservable<Document> Measure(SingleRequestData[] documents, CancellationToken token)
        {
            return Observable.Empty<Document>();
        }

        public Task<Document> Measure(string text, CancellationToken token)
        {
            return Task.FromResult((Document)null);
        }

        public Task<double?> Measure(string text)
        {
            return Task.FromResult((double?)null);
        }

        public IObservable<(string, double?)> Measure((string Id, string Text)[] items, CancellationToken token)
        {
            return Observable.Empty<(string, double?)>();
        }
    }
}