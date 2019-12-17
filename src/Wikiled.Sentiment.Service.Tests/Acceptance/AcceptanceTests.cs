using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Net.Client;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service;
using Wikiled.Server.Core.Testing.Server;
using Wikiled.WebSockets.Client.Definition;

namespace Wikiled.Sentiment.Service.Tests.Acceptance
{
    [TestFixture]
    public class AcceptanceTests
    {
        private ServerWrapper wrapper;

        private ISentimentAnalysis analysis;

        private IClient client;

        [OneTimeSetUp]
        public void SetUp()
        {
            wrapper = ServerWrapper.Create<Startup>(TestContext.CurrentContext.TestDirectory, services => { });
            var services = new ServiceCollection();
            services.RegisterModule<SentimentApiModule>();
            var provider = services.BuildServiceProvider();
            analysis = provider.GetRequiredService<ISentimentAnalysis>();
            client = provider.GetRequiredService<IClient>();
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
            await client.Connect(new Uri("Test")).ConfigureAwait(false);
            analysis.Settings.CleanText = true;
            analysis.Settings.Domain = "TwitterMarket";

            var result = await analysis.Measure(
                             "This market is so bad and it will get worse",
                             CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(10, result.TotalWords);
            Assert.AreEqual(1, result.Stars);
            Assert.AreEqual(1, result.Sentences.Count);
        }
    }
}
