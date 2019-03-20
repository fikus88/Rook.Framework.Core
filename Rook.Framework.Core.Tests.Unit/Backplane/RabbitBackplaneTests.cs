using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;

namespace Rook.Framework.Core.Tests.Unit.Backplane
{
    [TestClass]
    public class RabbitBackplaneTests
    {
        [TestMethod]
        public void Send_RecordsChannelBeingOpened_WhenANewOneIsCreated()
        {
            var rabbitConnectionManagerMock = new Mock<IRabbitMqConnectionManager>();
            var backplaneMetricsMock = new Mock<IBackplaneMetrics>();
            var connectionMock = new Mock<IConnection>();
            var modelMock = new Mock<IModel>();

            modelMock.Setup(x => x.QueueDeclare("", false, true, true, null)).Returns(new QueueDeclareOk("", 0, 0));
            connectionMock.Setup(x => x.CreateModel()).Returns(modelMock.Object);

            rabbitConnectionManagerMock.Setup(x => x.Connection).Returns(connectionMock.Object);

            var sut = new RabbitBackplane(Mock.Of<ILogger>(), rabbitConnectionManagerMock.Object, null, backplaneMetricsMock.Object);
            sut.Start();
            sut.Send(1);

            backplaneMetricsMock.Verify(x => x.RecordNewBackplaneChannel(), Times.Exactly(2));
        }
    }
}
