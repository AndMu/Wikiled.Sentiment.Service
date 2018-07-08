using System;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Data.Review;

namespace Wikiled.Sentiment.Service.Logic
{
    public interface IReviewSink : IDisposable
    {
        IObservable<IParsedDocumentHolder> Reviews { get; }

        void AddReview(SingleRequestData review, bool doCleanup);

        void Completed();
    }
}