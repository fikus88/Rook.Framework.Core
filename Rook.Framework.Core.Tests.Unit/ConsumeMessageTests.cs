using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client.Events;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit
{
    [TestClass]
    public class ConsumeMessageTests
    {
        private readonly IServiceMetrics _metrics = Mock.Of<IServiceMetrics>();

        [TestMethod]
        public void UndeserialisableMessageIsAcked()
        {
            ArtificialQueueWrapper qw = new ArtificialQueueWrapper();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();
            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            MessageSubscriber subscriber = new MessageSubscriber(qw,  log.Object, config.Object, _metrics,mim.Object, Mock.Of<IContainerFacade>());
            BasicDeliverEventArgs eventDetails = new BasicDeliverEventArgs()
            {
                Body = Encoding.ASCII.GetBytes("{message:\"Hello World\"}")
            };
            subscriber.ConsumeMessage(null, eventDetails);
            Assert.IsTrue(qw.AcknowledgedMessages.Contains(eventDetails));
        }

        [TestMethod]
        public void MessageIsRepublished()
        {
            ArtificialQueueWrapper qw = new ArtificialQueueWrapper();
            Mock<ILogger> log = new Mock<ILogger>();
            Mock<IMethodInspectorManager> mim = new Mock<IMethodInspectorManager>();

            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            config.Setup(x => x.Get("UseStructureMap", false)).Returns(true);
            var container = new Container(new MicroserviceRegistry(typeof(ConsumeMessageTests).Assembly));
            container.Configure(x => { x.For<IQueueWrapper>().ClearAll().Use(qw); });

            var containerFacade = new ContainerFacade(container, config.Object);
            
            MessageSubscriber subscriber = new MessageSubscriber(qw, log.Object, config.Object, _metrics, mim.Object, containerFacade);
            Guid messageGuid = Guid.NewGuid();
            BasicDeliverEventArgs eventDetails = new BasicDeliverEventArgs()
            {
                Body = Encoding.ASCII.GetBytes("{uuid:\"" + messageGuid + "\", method:\"GetFish\", source:\"Nowhere\", need:\"redherring\",publishedTime:\"" + DateTime.UtcNow.ToString("s") + "\", lastModifiedBy:\"test\", lastModifiedTime:\"" + DateTime.UtcNow.ToString("s") + "\"}")
            };
            subscriber.ConsumeMessage(null, eventDetails);
            Thread.Sleep(50);
            Assert.AreEqual(1, qw.AcknowledgedMessages.Count);
            Assert.IsTrue(qw.AcknowledgedMessages.Contains(eventDetails));
            Assert.AreEqual(1, qw.PublishedMessages.Count);
        }

        [QueryHandler("GetFish")]
        public class GetFishHandler : IMessageHandler2<string, bool>
        {
            public CompletionAction Handle(Message<string, bool> message)
            {
                message.Solution = new[] { true };
                return CompletionAction.Republish;
            }
        }

        public class ArtificialQueueWrapper : IQueueWrapper
        {
            public void PublishMessage<TNeed, TSolution>(Message<TNeed, TSolution> message, Guid uuid = default)
            {
                PublishedMessages[uuid] = message;
            }

            public void Start(string topic = QueueConstants.DefaultRoutingKey)
            {
                throw new NotImplementedException();
            }

            public string StartMessageConsumer(AsyncEventHandler<BasicDeliverEventArgs> consumeMessageHandler)
            {
                throw new NotImplementedException();
            }
            
            public void Stop()
            {
                throw new NotImplementedException();
            }

            public void StopMessageConsumer(string consumerTag)
            {
                throw new NotImplementedException();
            }

            public void AcknowledgeMessage(BasicDeliverEventArgs eventDetails)
            {
                AcknowledgedMessages.Add(eventDetails);
            }

            public AutoDictionary<Guid, object> PublishedMessages = new AutoDictionary<Guid, object>();
            public List<BasicDeliverEventArgs> AcknowledgedMessages = new List<BasicDeliverEventArgs>();

            public StartupPriority StartupPriority => throw new NotImplementedException();

            public void RejectMessage(BasicDeliverEventArgs eventDetails)
            {
                AcknowledgedMessages.Add(eventDetails);
            }

            public uint MessageCount(string queueName)
            {
                throw new NotImplementedException();
            }
        }
    }
}