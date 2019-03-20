using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit.Services
{
    [TestClass]
    public class ServiceTests
    {
        private IService Sut => new Service(Mock.Of<ILogger>(), _containerFacade);
        private Mock<IStartStoppable> _mockMessageSubscriber;
        private Mock<IStartStoppable> _mockQueueWrapper;
        private Mock<IStartStoppable> _mockScaling;
        private Mock<IStartable> _mockBackplane;
        private Container _container;
        private ContainerFacade _containerFacade;
        private Mock<IConfigurationManager> _configurationManagerMock;

        [TestInitialize]
        public void SetUp()
        {
            _mockQueueWrapper = new Mock<IStartStoppable>(MockBehavior.Strict);
            _mockQueueWrapper.Setup(x => x.StartupPriority).Returns(StartupPriority.High);

            _mockBackplane = new Mock<IStartable>(MockBehavior.Strict);
            _mockBackplane.Setup(x => x.StartupPriority).Returns(StartupPriority.Normal);

            _mockMessageSubscriber = new Mock<IStartStoppable>(MockBehavior.Strict);
            _mockMessageSubscriber.Setup(x => x.StartupPriority).Returns(StartupPriority.Low);

            _mockScaling = new Mock<IStartStoppable>(MockBehavior.Strict);
            _mockScaling.Setup(x => x.StartupPriority).Returns(StartupPriority.Lowest);

            _configurationManagerMock = new Mock<IConfigurationManager>();
            _configurationManagerMock.Setup(x => x.Get("UseStructureMap", false)).Returns(true);

            _container = new Container(new MicroserviceRegistry(typeof(ServiceTests).Assembly));
            
            _containerFacade = new ContainerFacade(_container, _configurationManagerMock.Object);
        }


        [TestMethod]
        public void CanBuildServiceFromContainer()
        {
            var container = new Container(new MicroserviceRegistry(typeof(ServiceTests).Assembly));
            var instance = container.GetInstance<IService>();

            Assert.AreEqual(instance.GetType(), typeof(Service));
        }

        [TestMethod]
        public void Start_queueWrapper_and_backplane_start_method_should_be_called_before_messageSubscriber_start()
        {
            var sequence = new MockSequence();

            _mockQueueWrapper.InSequence(sequence).Setup(m => m.Start());
            _mockBackplane.InSequence(sequence).Setup(x => x.Start());
            _mockMessageSubscriber.InSequence(sequence).Setup(m => m.Start());
            _mockScaling.InSequence(sequence).Setup(m => m.Start());

            _container.Configure(x =>
            {
                x.For<IStartable>().ClearAll();
                x.For<IStartStoppable>().ClearAll();

                x.For<IStartable>().Add(_mockMessageSubscriber.Object);
                x.For<IStartable>().Add(_mockQueueWrapper.Object);

                x.For<IStartable>().Add(_mockBackplane.Object);

                x.For<IStartable>().Add(_mockScaling.Object);
                x.For<IStartStoppable>().Add(_mockScaling.Object);

                x.For<IStartStoppable>().Add(_mockMessageSubscriber.Object);
                x.For<IStartStoppable>().Add(_mockQueueWrapper.Object);
            });

            _containerFacade = new ContainerFacade(_container, _configurationManagerMock.Object);

            Sut.Start();
            
            _mockQueueWrapper.Verify(x => x.Start(), Times.Once);
            _mockBackplane.Verify(x => x.Start(), Times.Once);
            _mockMessageSubscriber.Verify(x => x.Start(), Times.Once);
            _mockScaling.Verify(x => x.Start(), Times.Once);
        }

    }
}
