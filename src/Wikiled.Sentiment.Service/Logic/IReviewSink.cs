using System;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Text.Data.Review;

namespace Wikiled.Sentiment.Service.Logic
{
    public interface IReviewSink : IDisposable
    {
        IObservable<IParsedDocumentHolder> Reviews { get; }

        IObservable<ProcessingContext> ParsedReviews { get; }

        void AddReview(SingleProcessingData review);
    }
}