using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Wikiled.Common.Net.Client;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service;
using Wikiled.Server.Core.Testing.Server;

namespace Wikiled.Sentiment.Service.Tests.Acceptance
{
    [TestFixture]
    public class AcceptanceTests
    {
        private ServerWrapper wrapper;

        [OneTimeSetUp]
        public void SetUp()
        {
            wrapper = ServerWrapper.Create<Startup>(TestContext.CurrentContext.TestDirectory, services => { });
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            wrapper.Dispose();
        }

        [Test]
        public async Task Version()
        {
            var response = await wrapper.ApiClient.GetRequest<RawResponse<string>>("api/sentiment/version", CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        public async Task Measure()
        {
            SentimentAnalysis analysis = new SentimentAnalysis(
                new NullLogger<SentimentAnalysis>(), 
                new StreamApiClientFactory(new NullLoggerFactory(), wrapper.Client, wrapper.Client.BaseAddress),
                new WorkRequest
                {
                    CleanText = true,
                    Domain = "TwitterMarket"
                });
            var result = await analysis.Measure(
                             "This market is so bad and it will get worse",
                             CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(10, result.TotalWords);
            Assert.AreEqual(1, result.Stars);
            Assert.AreEqual(1, result.Sentences.Count);
        }
    }
}
