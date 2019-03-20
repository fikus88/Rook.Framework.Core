using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client.Events;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.ResponseHandlers;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Tests.Unit
{
    [TestClass]
    public class ProcessAcceptanceBehavioursTests
    {
        private IServiceMetrics metrics = Mock.Of<IServiceMetrics>();

        [TestMethod]
        public void RejectOnErrorAlwaysRejectsWhenErrorsArePresent()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();

            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics,mim.Object, Mock.Of<IContainerFacade>());
            HandlerAttribute handlerAttribute = new HandlerAttribute("Blah"){ErrorsBehaviour = ErrorsBehaviour.RejectIfErrorsExist, AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithSolution};
            object message = new Message<string, bool>(){Errors = new List<ResponseError>(){new ResponseError()}};
            Assert.IsFalse( subscriber.ProcessAcceptanceBehaviours(handlerAttribute,typeof(Message<string,bool>),message));
        }

        [TestMethod]
        public void RejectOnErrorDoesNotRejectWhenErrorsAreNotPresent()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();

            MessageSubscriber subscriber = new MessageSubscriber(qw.Object,  log.Object, config.Object, metrics,mim.Object, Mock.Of<IContainerFacade>());
            HandlerAttribute handlerAttribute = new HandlerAttribute("Blah") { ErrorsBehaviour = ErrorsBehaviour.RejectIfErrorsExist, AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution };
            object message = new Message<string, bool>() { };
            Assert.IsTrue(subscriber.ProcessAcceptanceBehaviours(handlerAttribute, typeof(Message<string, bool>), message));
        }

        [TestMethod]
        public void AcceptOnErrorAlwaysAcceptsWhenErrorsArePresent()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());
            HandlerAttribute handlerAttribute = new HandlerAttribute("Blah") { ErrorsBehaviour = ErrorsBehaviour.AcceptOnlyIfErrorsExist, AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution };
            object message = new Message<string, bool>() { Errors = new List<ResponseError>() { new ResponseError() } };
            Assert.IsTrue(subscriber.ProcessAcceptanceBehaviours(handlerAttribute, typeof(Message<string, bool>), message));
        }

        [TestMethod]
        public void AcceptOnErrorAlwaysRejectsWhenErrorsAreNotPresent()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());

            HandlerAttribute handlerAttribute = new HandlerAttribute("Blah") { ErrorsBehaviour = ErrorsBehaviour.AcceptOnlyIfErrorsExist, AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution };
            object message = new Message<string, bool>() {  };
            Assert.IsFalse(subscriber.ProcessAcceptanceBehaviours(handlerAttribute, typeof(Message<string, bool>), message));
        }

        [TestMethod]
        public void OnlyWithSolutionAlwaysRejectsWhenSolutionIsMissing()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());

            HandlerAttribute handlerAttribute = new HandlerAttribute("Blah") { AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithSolution };
            object message = new Message<string, bool>() { };
            Assert.IsFalse(subscriber.ProcessAcceptanceBehaviours(handlerAttribute, typeof(Message<string, bool>), message));
        }

        [TestMethod]
        public void OnlyWithoutSolutionAlwaysRejectsWhenSolutionIsPresent()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());

            HandlerAttribute handlerAttribute = new HandlerAttribute("Blah") { AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution };
            object message = new Message<string, bool>() { Solution=new []{true} };
            Assert.IsFalse(subscriber.ProcessAcceptanceBehaviours(handlerAttribute, typeof(Message<string, bool>), message));
        }
    }

    [TestClass]
    public class MethodInspectorTests
    {
        private IServiceMetrics metrics = Mock.Of<IServiceMetrics>();


        [TestMethod]
        public void EmptyMessageReturnsNullMethodInspector()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());


            BasicDeliverEventArgs bdea = new BasicDeliverEventArgs
            {
                Body = Encoding.ASCII.GetBytes("")
            };
            MessageSubscriber.MethodInspector mi = subscriber.GetMethodInspector(bdea);
            Assert.IsNull(mi);
        }

        [TestMethod]
        public void BadMessageReturnsNullMethodInspector()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());


            BasicDeliverEventArgs bdea = new BasicDeliverEventArgs
            {
                Body = Encoding.ASCII.GetBytes("Hello, Dave!")
            };
            MessageSubscriber.MethodInspector mi = subscriber.GetMethodInspector(bdea);
            Assert.IsNull(mi);
        }

        [TestMethod]
        public void GoodMessageReturnsGoodMethodInspector()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());


            BasicDeliverEventArgs bdea = new BasicDeliverEventArgs
            {
                Body = Encoding.ASCII.GetBytes("{" + $"uuid:\"{Guid.NewGuid()}\",method:\"Hello\",need:\"1234\"" + "}")
            };
            MessageSubscriber.MethodInspector mi = subscriber.GetMethodInspector(bdea);
            Assert.IsNotNull(mi);
        }

        [TestMethod]
        public void LongMessageReturnsGoodMethodInspector()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());


            BasicDeliverEventArgs bdea = new BasicDeliverEventArgs
            {
                Body = Encoding.ASCII.GetBytes("{" + $"uuid:\"{Guid.NewGuid()}\",method:\"Hello\",need:\"12345678901234567890123456789012345678901234567890123456789012345678901234567890\"" + "}")
            };
            MessageSubscriber.MethodInspector mi = subscriber.GetMethodInspector(bdea);
            Assert.IsNotNull(mi);
        }

        [TestMethod]
        public void TwoIdenticalMessagesReturnSameHashCodes()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());


            Guid messageGuid = Guid.NewGuid();

            BasicDeliverEventArgs bdea1 = new BasicDeliverEventArgs();
            bdea1.Body = Encoding.ASCII.GetBytes("{" + $"uuid:\"{messageGuid}\",method:\"Hello\",need:\"1234\"" + "}");
            MessageSubscriber.MethodInspector mi1 = subscriber.GetMethodInspector(bdea1);
            Assert.IsNotNull(mi1);

            BasicDeliverEventArgs bdea2 = new BasicDeliverEventArgs();
            bdea2.Body = Encoding.ASCII.GetBytes("{" + $"uuid:\"{messageGuid}\",method:\"Hello\",need:\"1234\"" + "}");
            MessageSubscriber.MethodInspector mi2 = subscriber.GetMethodInspector(bdea2);
            Assert.IsNotNull(mi2);

            Assert.AreEqual(mi1.Hash, mi2.Hash);
        }

        [TestMethod]
        public void TwoMessagesWithDifferentNeedsReturnDifferentHashCodes()
        {
            Mock<IQueueWrapper> qw = new Mock<IQueueWrapper>();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw.Object, log.Object, config.Object, metrics, mim.Object, Mock.Of<IContainerFacade>());


            Guid messageGuid = Guid.NewGuid();

            BasicDeliverEventArgs bdea1 = new BasicDeliverEventArgs();
            bdea1.Body = Encoding.ASCII.GetBytes("{" + $"uuid:\"{messageGuid}\",method:\"Hello\",need:\"1234\"" + "}");
            MessageSubscriber.MethodInspector mi1 = subscriber.GetMethodInspector(bdea1);
            Assert.IsNotNull(mi1);

            BasicDeliverEventArgs bdea2 = new BasicDeliverEventArgs();
            bdea2.Body = Encoding.ASCII.GetBytes("{" + $"uuid:\"{messageGuid}\",method:\"Hello\",need:\"1233\"" + "}");
            MessageSubscriber.MethodInspector mi2 = subscriber.GetMethodInspector(bdea2);
            Assert.IsNotNull(mi2);

            Assert.AreNotEqual(mi1.Hash, mi2.Hash);
        }

        [TestMethod]
        public void WrapAroundInt()
        {
            int x = int.MaxValue;
            x += 1;
            Assert.AreEqual(int.MinValue, x);
        }

    }
}