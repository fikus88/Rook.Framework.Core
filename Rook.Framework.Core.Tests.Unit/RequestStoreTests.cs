using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client.Framing;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit
{
    [TestClass]
    public class RequestStoreTests
    {
        private Mock<IDateTimeProvider> _dateTimeProvider;
        private Mock<IQueueWrapper> _queueWrapper;
        private Mock<ILogger> _logger;
        private Mock<IRequestMatcher> _requestMatcher;
        private Mock<IConfigurationManager> _configurationManager;
        private Mock<IBackplane> _backplane;
        
        [TestInitialize]
        public void BeforeEachTest()
        {
            var mockRepo = new MockRepository(MockBehavior.Default);
            _dateTimeProvider = mockRepo.Create<IDateTimeProvider>();
            _queueWrapper = mockRepo.Create<IQueueWrapper>();
            _logger = mockRepo.Create<ILogger>();
            _requestMatcher = mockRepo.Create<IRequestMatcher>();
            _configurationManager = mockRepo.Create<IConfigurationManager>();
            _backplane = mockRepo.Create<IBackplane>();
        }

        private RequestStore Sut => new RequestStore(_dateTimeProvider.Object, _queueWrapper.Object, _logger.Object, _requestMatcher.Object, _configurationManager.Object,
            _backplane.Object
            );

        [TestMethod]
        public void PublishAndWaitForTypedResponse_ReturnsEmptyObject_WhenResponseIsNotReceivedFromBus()
        {
            var response = Sut.PublishAndWaitForTypedResponse(new Message<string, TestSolution>
            {
                Need = "Test"
            });

            Assert.IsNull(response.Solution);
        }

        [TestMethod]
        public void CanBeCreatedFromContainer()
        {
            var container = new Container(new MicroserviceRegistry(typeof(RequestStoreTests).Assembly));
            var result = container.GetInstance<IRequestStore>();

            Assert.AreEqual(typeof(RequestStore), result.GetType());
        }
    }

    public class TestSolution { }
}
