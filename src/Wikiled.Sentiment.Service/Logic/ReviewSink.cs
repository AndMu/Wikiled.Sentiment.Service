using System;
using System.Reactive.Subjects;
using NLog;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.Parser;

namespace Wikiled.Sentiment.Service.Logic
{
    public class ReviewSink : IReviewSink
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Subject<IParsedDocumentHolder> reviews = new Subject<IParsedDocumentHolder>();

        private readonly ITextSplitter splitter;

        public ReviewSink(ITextSplitter splitter)
        {
            Guard.NotNull(() => splitter, splitter);
            this.splitter = splitter;
        }

        public IObservable<IParsedDocumentHolder> Reviews => reviews;

        public IObservable<ProcessingContext> ParsedReviews { get; set; }

        public void AddReview(SingleProcessingData review)
        {
            if (review.Date == null)
            {
                review.Date = DateTime.Now;
            }

            reviews.OnNext(new ParsingDocumentHolder(splitter, review));
        }

        public void Dispose()
        {
            logger.Debug("Dispose");
            reviews?.Dispose();
        }
    }
}
