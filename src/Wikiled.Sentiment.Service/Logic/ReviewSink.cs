using System;
using System.Reactive.Subjects;
using NLog;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Text.Analysis.Twitter;

namespace Wikiled.Sentiment.Service.Logic
{
    public class ReviewSink : IReviewSink
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Subject<IParsedDocumentHolder> reviews = new Subject<IParsedDocumentHolder>();

        private readonly ITextSplitter splitter;

        private readonly MessageCleanup cleanup = new MessageCleanup();

        public ReviewSink(ITextSplitter splitter)
        {
            Guard.NotNull(() => splitter, splitter);
            this.splitter = splitter;
        }

        public IObservable<IParsedDocumentHolder> Reviews => reviews;

        public IObservable<ProcessingContext> ParsedReviews { get; set; }

        public void AddReview(SingleProcessingData review, bool doCleanup)
        {
            if (review.Date == null)
            {
                review.Date = DateTime.Now;
            }

            review.Text = doCleanup ? cleanup.Cleanup(review.Text) : review.Text;
            reviews.OnNext(new ParsingDocumentHolder(splitter, review));
        }

        public void Dispose()
        {
            logger.Debug("Dispose");
            reviews?.Dispose();
        }
    }
}
