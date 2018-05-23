using System;
using System.Reactive.Subjects;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.Parser;

namespace Wikiled.Sentiment.Service.Logic
{
    public class ReviewSink : IReviewSink
    {
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
            reviews.OnNext(new ParsingDocumentHolder(splitter, review));
        }

        public void Dispose()
        {
            reviews?.Dispose();
        }
    }
}
