using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;

namespace Rook.Framework.Core.Tests.Unit.Application.Bus
{
    [TestClass]
    public class RabbitMqWrapperTests
    {
        private Mock<IDateTimeProvider> _mockDateTimeProvider;
        private Mock<ILogger> _mockLogger;
        private Mock<IConfigurationManager> _mockConfigurationManager;
        private Mock<IRabbitMqConnectionManager> _mockConnectionFactory;
        private Mock<IServiceMetrics> _mockServiceMetrics;
        private RabbitMqWrapper _sut;
        private Mock<IModel> _mockModel;

        [TestInitialize]
        public void SetUp()
        {
            _mockDateTimeProvider = new Mock<IDateTimeProvider>();
            _mockLogger = new Mock<ILogger>();
            _mockConfigurationManager = new Mock<IConfigurationManager>();
            _mockConnectionFactory = new Mock<IRabbitMqConnectionManager>();
            _mockServiceMetrics = new Mock<IServiceMetrics>();
            _mockModel = new Mock<IModel>();

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(m => m.CreateModel()).Returns(_mockModel.Object);
            _mockConnectionFactory.Setup(m => m.Connection).Returns(mockConnection.Object);

            _sut = CreateSut();

        }

        [TestMethod]
        public void Start_should_not_call_BasicQos_on_model_if_MaximumConcurrency_config_is_not_set()
        {
            _sut.Start();

            _mockModel.Verify(m => m.BasicQos(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void Start_should_call_BasicQos_on_model_if_MaximumConcurrency_config_is_set()
        {
            _mockConfigurationManager.Setup(m => m.Get<ushort>("MaximumConcurrency", 0)).Returns(25);
            _sut = CreateSut();

            _sut.Start();

            _mockModel.Verify(m => m.BasicQos(0, 25, false), Times.Once);
        }

        [TestMethod]
        public void PublishMessage_RecordsNewMainChannelBeingCreated_WhenPublishing()
        {
            var sut = CreateSut();

            sut.Start();
            sut.PublishMessage(new Message<object, object>
            {
                Method = "TestMethod",
                Need = 1
            });

            _mockServiceMetrics.Verify(x => x.RecordNewMainChannel(), Times.Exactly(2));
        }

        public RabbitMqWrapper CreateSut()
        {
            return new RabbitMqWrapper(_mockDateTimeProvider.Object,
                _mockLogger.Object,
                _mockConfigurationManager.Object,
                _mockConnectionFactory.Object,
                _mockServiceMetrics.Object);
        }
    }
}
