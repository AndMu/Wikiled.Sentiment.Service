using System;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wikiled.Common.Net.Client;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service;
using Wikiled.Sentiment.Api.Service.Module;
using Wikiled.Sentiment.Api.Service.Mqtt;
using Wikiled.Server.Core.Testing.Server;

namespace Wikiled.Sentiment.Service.Tests.Acceptance
{
    [TestFixture]
    public class AcceptanceTests
    {
        private ServerWrapper wrapper;

        private ISentimentAnalysisSetup analysisSetup;

        [OneTimeSetUp]
        public void SetUp()
        {
            wrapper = ServerWrapper.Create<Startup>(TestContext.CurrentContext.TestDirectory, services => { });
            var services = new ServiceCollection();
            services.RegisterModule(
                new SentimentApiModule(new MqttConnectionInfo(new Uri("http://localhost:1883/mqtt"), "TestId")));
            var provider = services.BuildServiceProvider();
            analysisSetup = provider.GetService<ISentimentAnalysisSetup>();
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
        [Ignore("Can't make to work")]
        public async Task Measure()
        {
            var request = new WorkRequest
            {
                CleanText = true,
                Domain = "TwitterMarket"
            };

            var result = await analysisSetup.Setup(request).Measure(
                             "This market is so bad and it will get worse",
                             CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(10, result.TotalWords);
            Assert.AreEqual(1, result.Stars);
            Assert.AreEqual(1, result.Sentences.Count);
            Assert.Fail();
        }
    }
}
