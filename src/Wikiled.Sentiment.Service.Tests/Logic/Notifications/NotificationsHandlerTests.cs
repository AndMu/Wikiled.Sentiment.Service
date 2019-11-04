using System;
using MQTTnet.Server;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Publishing;
using NUnit.Framework.Internal;
using Wikiled.Common.Reflection;
using Wikiled.Common.Testing.Utilities.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Service.Logic.Notifications;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Common.Utilities.Helpers;
using Wikiled.Sentiment.Analysis.Pipeline;

namespace Wikiled.Sentiment.Service.Tests.Logic.Notifications
{
    [TestFixture]
    public class NotificationsHandlerTests
    {
        private readonly ILogger<NotificationsHandler> logger = TestLogger.Create<NotificationsHandler>();
        private Mock<IMqttServer> mockMqttServer;
        private IJsonSerializer jsonSerializer;

        private NotificationsHandler instance;

        [SetUp]
        public void SetUp()
        {
            mockMqttServer = new Mock<IMqttServer>();
            jsonSerializer = new BasicJsonSerializer(MemoryStreamInstances.MemoryStream);;

            mockMqttServer.Setup(item => item.PublishAsync(It.IsAny<MqttApplicationMessage>(), CancellationToken.None))
                          .Returns(Task.FromResult(new MqttClientPublishResult() { ReasonCode = MqttClientPublishReasonCode.Success }));
            instance = CreateNotificationsHandler();
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<NotificationsHandler>(MemoryStreamInstances.MemoryStream);
        }

        [Test]
        public async Task PublishResults()
        {
            await instance.PublishResults("Test", new List<ProcessingContext>()).ConfigureAwait(false);
            mockMqttServer.Verify(item => item.PublishAsync(It.IsAny<MqttApplicationMessage>(), CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task SendMessage()
        {
            await instance.SendUserMessage("Test", "Message", "Data");
            Assert.Fail();
        }

        private NotificationsHandler CreateNotificationsHandler()
        {
            return new NotificationsHandler(logger, mockMqttServer.Object, jsonSerializer, MemoryStreamInstances.MemoryStream);
        }
    }
}