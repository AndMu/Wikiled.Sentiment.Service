using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using Moq;
using MQTTnet;
using NUnit.Framework;
using Wikiled.Common.Testing.Utilities.Logging;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service.Flow;
using Wikiled.Sentiment.Api.Service.Mqtt;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Tests.Service.Flow
{
    [TestFixture]
    public class SentimentFlowTests : ReactiveTest
    {
        private ILogger<SentimentFlow> logger = TestLogger.Create<SentimentFlow>();
        private TestScheduler scheduler;
        private MqttConnectionInfo connectionInfo;
        private Mock<IJsonSerializer> mockJsonSerializer;
        private Mock<IMqttConnection> connection;
        private Mock<IMqttSubscription> subscription;

        private SentimentFlow instance;

        private WorkRequest request;

        private ITestableObserver<Document> result;

        private ITestableObservable<MqttApplicationMessage> messages;

        private ITestableObservable<MqttApplicationMessage> doneMessages;

        private ITestableObservable<MqttApplicationMessage> infoMessages;

        private ITestableObservable<MqttApplicationMessage> errorMessages;

        [SetUp]
        public void SetUp()
        {
            scheduler = new TestScheduler();
            request = new WorkRequest();
            request.Documents = new[]
            {
                new SingleRequestData(),
                new SingleRequestData(),
                new SingleRequestData()
            };

            result = scheduler.CreateObserver<Document>();

            connection = new Mock<IMqttConnection>();
            connection.Setup(item => item.Connect(connectionInfo, CancellationToken.None)).Returns(Task.CompletedTask);
            subscription = new Mock<IMqttSubscription>();
            connection.Setup(item => item.CreateSubscription("Sentiment/Result/Test1")).Returns(subscription.Object);
            subscription.Setup(item => item.Subscription).Returns(() => messages);
            messages = scheduler.CreateHotObservable<MqttApplicationMessage>();

            var sub = new Mock<IMqttSubscription>();
            connection.Setup(item => item.CreateSubscription("Sentiment/Done/Test1")).Returns(sub.Object);
            doneMessages = scheduler.CreateHotObservable<MqttApplicationMessage>();
            sub.Setup(item => item.Subscription).Returns(() => doneMessages);

            sub = new Mock<IMqttSubscription>();
            connection.Setup(item => item.CreateSubscription("Error/Test1")).Returns(sub.Object);
            errorMessages = scheduler.CreateHotObservable<MqttApplicationMessage>();
            sub.Setup(item => item.Subscription).Returns(() => errorMessages);

            sub = new Mock<IMqttSubscription>();
            connection.Setup(item => item.CreateSubscription("Message/Test1")).Returns(sub.Object);
            infoMessages = scheduler.CreateHotObservable<MqttApplicationMessage>();
            sub.Setup(item => item.Subscription).Returns(() => infoMessages);

            connectionInfo = new MqttConnectionInfo(new Uri("http://localhost"), "Test1");
            mockJsonSerializer = new Mock<IJsonSerializer>();
            instance = CreateSentimentFlow();
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<SentimentFlow>();
        }

        [Test]
        public void Timeout()
        {
            instance.Start(request, CancellationToken.None).Subscribe(result);
            scheduler.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
            result.Messages.AssertEqual(OnCompleted<Document>(TimeSpan.FromMinutes(5).Ticks));
        }

        private SentimentFlow CreateSentimentFlow()
        {
            return new SentimentFlow(
                logger,
                scheduler,
                () => connection.Object,
                connectionInfo,
                mockJsonSerializer.Object);
        }
    }
}