using System;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Text.Analysis.Twitter;

namespace Wikiled.Sentiment.Service.Logic
{
    public class ReviewSink : IReviewSink
    {
        private ILogger<ReviewSink> logger;

        private readonly Subject<IParsedDocumentHolder> reviews = new Subject<IParsedDocumentHolder>();

        private readonly ITextSplitter splitter;

        private readonly MessageCleanup cleanup = new MessageCleanup();

        public ReviewSink(ILoggerFactory loggerFactory, ITextSplitter splitter)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<ReviewSink>();
            this.splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
        }

        public IObservable<IParsedDocumentHolder> Reviews => reviews;

        public void AddReview(SingleRequestData review, bool doCleanup)
        {
            if (review.Date == null)
            {
                review.Date = DateTime.Now;
            }

            if (review.Id == null)
            {
                review.Id = Guid.NewGuid().ToString();
            }

            review.Text = doCleanup ? cleanup.Cleanup(review.Text) : review.Text;
            SingleProcessingData data = new SingleProcessingData();
            data.Author = review.Author;
            data.Date = review.Date;
            data.Id = review.Id;
            data.Text = review.Text;
            reviews.OnNext(new ParsingDocumentHolder(splitter, data));
        }

        public void Completed()
        {
            reviews.OnCompleted();
        }

        public void Dispose()
        {
            logger.LogDebug("Dispose");
            reviews.OnCompleted();
            reviews?.Dispose();
        }
    }
}
