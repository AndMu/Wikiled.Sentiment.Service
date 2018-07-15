using System;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Parser;

namespace Wikiled.Sentiment.Service.Tests.Logic
{
    [TestFixture]
    public class ReviewSinkTests
    {
        private Mock<ITextSplitter> mockTextSplitter;

        private ReviewSink instance;

        [SetUp]
        public void SetUp()
        {
            mockTextSplitter = new Mock<ITextSplitter>();
            instance = CreateReviewSink();
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(() => new ReviewSink(new NullLoggerFactory(), null));
            Assert.Throws<ArgumentNullException>(() => new ReviewSink(null, mockTextSplitter.Object));
        }

        private ReviewSink CreateReviewSink()
        {
            return new ReviewSink(new NullLoggerFactory(), mockTextSplitter.Object);
        }
    }
}