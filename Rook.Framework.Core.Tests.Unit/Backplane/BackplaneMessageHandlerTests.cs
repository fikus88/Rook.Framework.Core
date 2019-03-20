using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Text;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Tests.Unit.Backplane
{
    [TestClass]
    public class BackplaneMessageHandlerTests
    {
        BackplaneMessageHandler _backplaneMessageHandler;
        Mock<IContainerFacade> _container;
        Mock<IBackplaneMetrics> _backplaneMetrics;

        Mock<IBackplaneConsumer> _typeOneConsumer;
        Mock<IBackplaneConsumer> _typeTwoConsumer;

        [TestInitialize]
        public void TestSetup()
        {
            _container = new Mock<IContainerFacade>();
            _backplaneMetrics = new Mock<IBackplaneMetrics>();

            _backplaneMessageHandler = new BackplaneMessageHandler(
                _container.Object,
                _backplaneMetrics.Object,
                Mock.Of<ILogger>());

            _typeOneConsumer = new Mock<IBackplaneConsumer>();
            _typeTwoConsumer = new Mock<IBackplaneConsumer>();

            _typeOneConsumer.Setup(x => x.ConsumesType).Returns(typeof(MessageTypeOne).GUID);
            _typeTwoConsumer.Setup(x => x.ConsumesType).Returns(typeof(MessageTypeTwo).GUID);

            _container.Setup(x => x.GetAllInstances<IBackplaneConsumer>()).Returns(new IBackplaneConsumer[] { _typeOneConsumer.Object, _typeTwoConsumer.Object });
            _container.Setup(x => x.GetByTypeGuid(typeof(MessageTypeOne).GUID)).Returns(typeof(MessageTypeOne));
            _container.Setup(x => x.GetByTypeGuid(typeof(MessageTypeTwo).GUID)).Returns(typeof(MessageTypeTwo));
        }

        [TestMethod]
        public void Handle_GivenMessageWithInvalidObjectType_ThrowsInvalidOperationException()
        {
            // An invalid object type is a type that is not mapped within the container. I.E A string.
            var message = GetMessage("a string value");

            Assert.ThrowsException<InvalidOperationException>(() => _backplaneMessageHandler.Handle(message));
        }

        [TestMethod]
        public void Handle_GivenValidMessage_CorrectConsumerIsInvoked()
        {
            var message = GetMessage(new MessageTypeTwo());

            _backplaneMessageHandler.Handle(message).Wait();

            _typeOneConsumer.Verify(x => x.Consume(It.IsAny<MessageTypeOne>()), Times.Never);
            _typeTwoConsumer.Verify(x => x.Consume(It.IsAny<MessageTypeTwo>()), Times.Once);
        }

        [TestMethod]
        public void Handle_GivenValidMessage_DurationToConsumeMessageIsRecorded()
        {
            _container.Setup(x => x.GetAllInstances<IBackplaneConsumer>()).Returns(new IBackplaneConsumer[] { new MyMessageConsumer() });
            _container.Setup(x => x.GetByTypeGuid(typeof(MyMessageConsumer).GUID)).Returns(typeof(MyMessageConsumer));

            var message = GetMessage(new MessageTypeOne());

            _backplaneMessageHandler.Handle(message).Wait();

            _backplaneMetrics.Verify(x => x.RecordProcessedMessage(nameof(MyMessageConsumer), It.IsAny<double>()), Times.Once);
        }

        [TestMethod]
        public void Handle_GivenMessageThatHasNoMatchingConsumer_ThrowsInvalidOperationException()
        {
            _container.Setup(x => x.GetAllInstances<IBackplaneConsumer>()).Returns(new IBackplaneConsumer[0]);

            var message = GetMessage(new MessageTypeOne());

            Assert.ThrowsException<InvalidOperationException>(() => _backplaneMessageHandler.Handle(message).Wait());
        }

        private byte[] GetMessage<T>(T message)
        {
            var messageWrapper = new ObjectWrapper<T>
            {
                Type = typeof(T).GUID,
                Data = message
            };

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageWrapper));
        }

        public class MessageTypeOne { }
        public class MessageTypeTwo { }

        class MyMessageConsumer : BackplaneConsumer<MessageTypeOne>
        {
            public override void Consume(MessageTypeOne value)
            {
                return;
            }
        }
    }
}
