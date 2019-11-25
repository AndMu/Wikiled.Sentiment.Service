using System;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.NLP.Repair;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Words;
using Wikiled.Text.Analysis.Twitter;

namespace Wikiled.Sentiment.Service.Logic
{
    public class DocumentConverter : IDocumentConverter
    {
        private readonly IMessageCleanup cleanup;

        private readonly ITextSplitter splitter;

        private readonly IWordFactory wordFactory;

        private readonly IContextSentenceRepairHandler sentenceRepair;

        public DocumentConverter(IMessageCleanup cleanup, ITextSplitter splitter, IWordFactory wordFactory, IContextSentenceRepairHandler sentenceRepair)
        {
            this.cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
            this.splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
            this.wordFactory = wordFactory ?? throw new ArgumentNullException(nameof(wordFactory));
            this.sentenceRepair = sentenceRepair ?? throw new ArgumentNullException(nameof(sentenceRepair));
        }

        public ParsingDocumentHolder Convert(SingleRequestData review, bool doCleanup)
        {
            if (review == null)
            {
                throw new ArgumentNullException(nameof(review));
            }

            review.Text = doCleanup ? cleanup.Cleanup(review.Text) : review.Text;
            var data = new SingleProcessingData();
            data.Author = review.Author;
            if (review.IsPositive.HasValue)
            {
                data.Stars = review.IsPositive.Value ? 5 : 1;
            }

            data.Date = review.Date;
            data.Id = review.Id;
            data.Text = review.Text;
            return new ParsingDocumentHolder(splitter, wordFactory, sentenceRepair, data);
        }
    }
}
