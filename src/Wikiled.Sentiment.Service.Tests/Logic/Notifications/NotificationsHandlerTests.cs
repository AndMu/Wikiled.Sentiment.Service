using MQTTnet.Server;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wikiled.Common.Testing.Utilities.Logging;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Service.Logic.Notifications;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Sentiment.Analysis.Pipeline;

namespace Wikiled.Sentiment.Service.Tests.Logic.Notifications
{
    [TestFixture]
    public class NotificationsHandlerTests
    {
        private ILogger<NotificationsHandler> logger = TestLogger.Create<NotificationsHandler>();
        private Mock<IMqttServer> mockMqttServer;
        private Mock<IJsonSerializer> mockJsonSerializer;

        private NotificationsHandler instance;

        [SetUp]
        public void SetUp()
        {
            mockMqttServer = new Mock<IMqttServer>();
            mockJsonSerializer = new Mock<IJsonSerializer>();
            instance = CreateNotificationsHandler();
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<NotificationsHandler>();
        }

        [Test]
        public async Task PublishResults()
        {
            await instance.PublishResults("Test", new List<ProcessingContext>());
            Assert.Fail();}

        [Test]
        public async Task SendError()
        {
            string userId = null;
            string message = null;

            await instance.SendError(userId, message);

            Assert.Fail();
        }

        [Test]
        public async Task SendMessage()
        {
            string userId = null;
            string message = null;

            await instance.SendMessage(
                userId,
                message);

            Assert.Fail();
        }

        private NotificationsHandler CreateNotificationsHandler()
        {
            return new NotificationsHandler(logger, mockMqttServer.Object, mockJsonSerializer.Object);
        }
    }
}