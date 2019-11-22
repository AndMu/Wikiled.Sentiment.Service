using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive.Concurrency;
using System.Threading;
using Microsoft.Reactive.Testing;
using Wikiled.Common.Testing.Utilities.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Service.Flow;
using Wikiled.Sentiment.Api.Service.Mqtt;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Sentiment.Api.Request;

namespace Wikiled.Sentiment.Service.Tests.Service.Flow
{
    [TestFixture]
    public class SentimentFlowTests
    {
        private ILogger<SentimentFlow> logger = TestLogger.Create<SentimentFlow>();
        private TestScheduler scheduler;
        private Mock<MqttConnectionInfo> mockMqttConnectionInfo;
        private Mock<IJsonSerializer> mockJsonSerializer;
        private Mock<IMqttConnection> _connection;

        private SentimentFlow instance;

        [SetUp]
        public void SetUp()
        {
            scheduler = new TestScheduler();
            _connection = new Mock<IMqttConnection>();
            mockMqttConnectionInfo = new Mock<MqttConnectionInfo>();
            mockJsonSerializer = new Mock<IJsonSerializer>();
            instance = CreateSentimentFlow();
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<SentimentFlow>();
        }

        [Test]
        public void Start()
        {
            Assert.Fail();
        }

        private SentimentFlow CreateSentimentFlow()
        {
            return new SentimentFlow(
                logger,
                scheduler,
                () => _connection.Object,
                mockMqttConnectionInfo.Object,
                mockJsonSerializer.Object);
        }
    }
}