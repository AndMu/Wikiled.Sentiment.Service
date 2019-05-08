using System;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Data.Review;

namespace Wikiled.Sentiment.Service.Logic
{
    public interface IReviewSink : IDisposable
    {
        SemaphoreSlim ProcessingSemaphore { get; set; }

        IObservable<IParsedDocumentHolder> Reviews { get; }

        Task AddReview(SingleRequestData review, bool doCleanup);

        void Completed();
    }
}