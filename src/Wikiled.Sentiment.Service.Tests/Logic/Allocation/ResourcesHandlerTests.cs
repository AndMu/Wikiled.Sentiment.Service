using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet.Server;
using NUnit.Framework;
using System.Threading.Tasks;
using MQTTnet.Server.Status;
using Wikiled.Common.Testing.Utilities.Logging;
using Wikiled.Common.Testing.Utilities.Reflection;
using Wikiled.Sentiment.Service.Logic.Allocation;
using Wikiled.Sentiment.Service.Logic.Notifications;

namespace Wikiled.Sentiment.Service.Tests.Logic.Allocation
{
    [TestFixture]
    public class ResourcesHandlerTests
    {
        private ILogger<NotificationsHandler> logger = TestLogger.Create<NotificationsHandler>();
        private Mock<IMqttServer> mockMqttServer;
        private Mock<IMqttClientStatus> status;
        private ResourcesHandler instance;

        [SetUp]
        public void SetUp()
        {
            mockMqttServer = new Mock<IMqttServer>();
            status = new Mock<IMqttClientStatus>();
            instance = CreateResourcesHandler();

            mockMqttServer.Setup(item => item.GetClientStatusAsync())
                          .Returns(Task.FromResult<IList<IMqttClientStatus>>(new List<IMqttClientStatus> { status.Object }));
        }

        [Test]
        public void Construct()
        {
            ConstructorHelper.ConstructorMustThrowArgumentNullException<ResourcesHandler>();
        }

        [Test]
        public async Task AllocateTraining()
        {
            status.Setup(item => item.ClientId).Returns("Test1");
            status.Setup(item => item.Endpoint).Returns("XXX");
            var result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task AllocateTrainingAllocated()
        {
            status.Setup(item => item.ClientId).Returns("Test1");
            status.Setup(item => item.Endpoint).Returns("XXX");
            await instance.AllocateTraining("Test1").ConfigureAwait(false);
            var result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AllocateTrainingTwoDifferent()
        {
            status.Setup(item => item.ClientId).Returns("Test1");
            status.SetupSequence(item => item.Endpoint).Returns("XXX").Returns("XXX1");
            var result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsTrue(result);
            result = await instance.AllocateTraining("Test2").ConfigureAwait(false);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task AllocateTrainingTwoDifferentSameIp()
        {
            status.Setup(item => item.ClientId).Returns("Test1");
            status.Setup(item => item.Endpoint).Returns("XXX");
            var result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsTrue(result);
            result = await instance.AllocateTraining("Test2").ConfigureAwait(false);
            Assert.IsFalse(result);
        }

        [Test]
        public void ReleaseNotTaken()
        {
            instance.Release("Test");
        }

        [Test]
        public async Task Release()
        {
            status.Setup(item => item.ClientId).Returns("Test1");
            status.Setup(item => item.Endpoint).Returns("XXX");

            var result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsTrue(result);
            result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsFalse(result);

            instance.Release("Test1");
            result = await instance.AllocateTraining("Test1").ConfigureAwait(false);
            Assert.IsTrue(result);
        }
        private ResourcesHandler CreateResourcesHandler()
        {
            return new ResourcesHandler(logger, mockMqttServer.Object);
        }
    }
}