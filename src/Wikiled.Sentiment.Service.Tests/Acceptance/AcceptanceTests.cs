using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using Wikiled.Common.Logging;
using Wikiled.Common.Net.Client;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service;
using Wikiled.Server.Core.Testing.Server;
using Wikiled.WebSockets.Client.Connection;
using Wikiled.WebSockets.Client.Definition;
using Wikiled.WebSockets.Client.Logic;

namespace Wikiled.Sentiment.Service.Tests.Acceptance
{
    [TestFixture]
    public class AcceptanceTests
    {
        private ServerWrapper wrapper;

        private ISentimentAnalysis analysis;

        private IClient client;

        private readonly Uri streamUri = new Uri("ws://localhost/stream");

        private IBasicSentimentAnalysis basicSentiment;

        [OneTimeSetUp]
        public void SetUp()
        {
            wrapper = ServerWrapper.Create<Startup>(
                TestContext.CurrentContext.TestDirectory,
                services =>
                {
                    services.AddSingleton(ApplicationLogging.LoggerFactory);
                    services.AddLogging(item => item.AddNLog());
                },
                (context, config) =>
                {
                    var env = context.HostingEnvironment;
                    
                    config.SetBasePath(env.ContentRootPath);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
                    config.AddEnvironmentVariables();
                    //config.AddInMemoryCollection(arrayDict);
                });
            var services = new ServiceCollection();
            services.RegisterModule(new SentimentApiModule(new Uri("http://localhost")) { Client = wrapper.Client });
            
            services.AddLogging(item => item.AddNLog());
            client = ConstructClient(services);
            services.AddSingleton(client);
            var provider = services.BuildServiceProvider();
            analysis = provider.GetRequiredService<ISentimentAnalysis>();
            basicSentiment = provider.GetRequiredService<IBasicSentimentAnalysis>();
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
        public async Task BasicMeasure()
        {
            var request = new SingleWorkRequest
            {
                Domain = "market",
                Review = new SingleRequestData { Text = "I like Text" }
            };

            var result = await basicSentiment.Measure(request, CancellationToken.None);
            Assert.AreEqual(5, result.Stars);

            result = await basicSentiment.Measure(request, CancellationToken.None);
            Assert.AreEqual(5, result.Stars);
        }

        [Test]
        public async Task Calculate()
        {
            var request = new SingleWorkRequest
            {
                Domain = "market",
                Review = new SingleRequestData { Text = "I like Text" }
            };

            var result = await basicSentiment.Calculate(request, CancellationToken.None);
            Assert.AreEqual(5, result.Stars);
            Assert.AreEqual(0.33, Math.Round(result.Positive, 2));
            Assert.AreEqual(0, result.Negative);
        }

        [Test]
        public async Task Measure()
        {
            await client.Connect(streamUri).ConfigureAwait(false);
            analysis.Settings.CleanText = true;
            analysis.Settings.Domain = "TwitterMarket";

            var result = await analysis.Measure(
                                            "This market is so bad and it will get worse",
                                            CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(10, result.TotalWords);
            Assert.AreEqual(1, result.Stars);
            Assert.AreEqual(1, result.Sentences.Count);
        }

        private IClient ConstructClient(ServiceCollection collection)
        {
            var provider = collection.BuildServiceProvider();
            return new ClientApi(provider.GetService<ILogger<ClientApi>>(),
                () => ConstructClientFactory(provider),
                TaskPoolScheduler.Default);
        }

        private async Task<IConnection> ConstructClientFactory(ServiceProvider provider)
        {
            var response = await wrapper.SocketClient.ConnectAsync(streamUri, CancellationToken.None).ConfigureAwait(false);
            var factory = new TestSocketFactory(response);
            return provider.GetService<Func<ISocketFactory, IConnection>>()(factory);
        }
    }
}
