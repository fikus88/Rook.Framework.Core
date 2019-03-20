using System;
using System.Collections.Generic;
using System.IO;
using Rook.Framework.Core.TestUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;

namespace Rook.Framework.Core.Tests.Unit
{
	[TestClass]
	public class RabbitMqWrapperTests
	{
        private readonly IServiceMetrics _metrics = Mock.Of<IServiceMetrics>();
        
		[TestMethod]
		[ExpectedException(typeof(RabbitMqWrapperException))]
		public void Start_WhenCalledWithoutQueueUri_RaisesException()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
			configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns((string)null);

            var connection = new Mock<IConnection>();
		    var connectionFactory = new Mock<IConnectionFactory>();
		    connectionFactory.Setup(x => x.CreateConnection()).Returns(connection.Object);

		    var rabbitMqConnectionManager = new RabbitMqConnectionManager(connectionFactory.Object, configurationManager.Object);
            
            var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, rabbitMqConnectionManager, _metrics);

			rabbitMqWrapper.Start();
		}

		[TestMethod]
		public void Start_WhenCalled_CallsCreateConnectionAndQueueBind()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
            
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();

			connectionManager.Verify(x => x.Connection, Times.Once);
			connection.Verify(x => x.CreateModel(), Times.Once);
			model.Verify(x => x.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Once);
		}

		[TestMethod]
		public void Stop_WhenCalledWithoutStart_DoesNotCallModelOrConnectionDispose()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
		    
            var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Stop();

			model.Verify(x => x.Dispose(), Times.Never);
			connection.Verify(x => x.Dispose(), Times.Never);
		}

		[TestMethod]
		public void Stop_WhenCalledAfterStart_CallsModelDispose()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
			configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);

		    var connectionFactory = new Mock<IConnectionFactory>();
		    connectionFactory.Setup(x => x.Uri).Returns(new Uri("amqp://test:test@localhost:5672"));
            connectionFactory.Setup(x => x.CreateConnection()).Returns(connection.Object);

		    var mqConnectionManager = new RabbitMqConnectionManager(connectionFactory.Object, configurationManager.Object);
		    
			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, mqConnectionManager, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.Stop();

			model.Verify(x => x.Dispose(), Times.Once);
		}

		[TestMethod]
		public void Stop_WhenConnectionCloseRaisesEndOfStreamException_DoesNotBubbleException()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
            var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			connection.Setup(x => x.Dispose()).Throws<EndOfStreamException>();
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.Stop();
		}

		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void Stop_WhenConnectionCloseRaisesNonEndOfStreamException_DoesBubbleException()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			connection.Setup(x => x.Dispose()).Throws<Exception>();

		    var connectionFactory = new Mock<IConnectionFactory>();
		    connectionFactory.Setup(x => x.Uri).Returns(new Uri("amqp://test:test@localhost:5672"));
            connectionFactory.Setup(x => x.CreateConnection()).Returns(connection.Object);

		    var rabbitMqConnectionManager = new RabbitMqConnectionManager(connectionFactory.Object, configurationManager.Object);

		    var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, rabbitMqConnectionManager, _metrics);

            rabbitMqWrapper.Start();
			rabbitMqWrapper.Stop();
            rabbitMqConnectionManager.Dispose();
		}

		[TestMethod]
		[ExpectedException(typeof(RabbitMqWrapperException))]
		public void DeleteQueue_WhenCalledWithoutStart_RaisesException()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
            
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.DeleteQueue();			
		}

		[TestMethod]
		public void DeleteQueue_WhenCalledAfterStart_CallsModelQueueDeleteNoWait()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
            configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
		    
            var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
			connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.DeleteQueue();

			model.Verify(x => x.QueueDeleteNoWait(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
		}

		[TestMethod]
		[ExpectedException(typeof(RabbitMqWrapperException))]
		public void PurgeQueue_WhenCalledWithoutStart_RaisesException()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.PurgeQueue();
		}

		[TestMethod]
		public void PurgeQueue_WhenCalledAfterStart_CallsModelQueuePurge()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.PurgeQueue();

			model.Verify(x => x.QueuePurge(It.IsAny<string>()), Times.Once);
		}

		[TestMethod]
		[ExpectedException(typeof(RabbitMqWrapperException))]
		public void StartMessageConsumer_WhenCalledWithoutStart_RaisesException()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");

		    var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);
			rabbitMqWrapper.StartMessageConsumer(new Mock<AsyncEventHandler<BasicDeliverEventArgs>>().Object);
		}

		[TestMethod]
		public void StartMessageConsumer_WhenCalledAfterStart_CallsLoggerTrace()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
            
			var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.StartMessageConsumer(new Mock<AsyncEventHandler<BasicDeliverEventArgs>>().Object);

			logger.Verify(x => x.Trace(It.Is<string>(s => s == "RabbitMqWrapper.StartMessageConsumer"),
				It.Is<LogItem>(li => li.Key == "Event" && li.Value() == "Message handler subscribed to queue"),
				It.Is<LogItem>(li => li.Key == "QueueName"),
				It.Is<LogItem>(li => li.Key == "ConsumerTag"),
				It.Is<LogItem>(li => li.Key == "HandlerMethod" && li.Value() == "Invoke")), Times.Once);
		}

	    [TestMethod]
	    public void StopMessageConsumer_WhenCalledWithoutStart_CallsLoggerWarn()
	    {
	        var dateTimeProvider = new Mock<IDateTimeProvider>();
	        var logger = new Mock<ILogger>();
	        var configurationManager = new Mock<IConfigurationManager>();
	        configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
	        
            var model = new Mock<IModel>();
	        var connection = new Mock<IConnection>();
	        connection.Setup(x => x.CreateModel()).Returns(model.Object);
	        var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

	        var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

	        rabbitMqWrapper.StopMessageConsumer("TestConsumerTag");

	        logger.Verify(x =>
	            x.Warn(It.Is<string>(s =>
	                s == "RabbitMqWrapper.Start must be run before calling StopMessageConsumer")));
	    }

        [TestMethod]
		public void StopMessageConsumer_WhenCalledWithNoConsumerTag_DoesNotCallLoggerTrace()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
		    
            var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.StopMessageConsumer(null);

			logger.Verify(x => x.Trace(It.IsAny<string>()), Times.Never);
		}

		[TestMethod]
		public void StopMessageConsumer_WhenCalledWithConsumerTag_CallsLoggerTrace()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			logger.Setup(x => x.Trace(It.IsAny<string>()));
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");

            var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			rabbitMqWrapper.Start();
			rabbitMqWrapper.StopMessageConsumer("TestConsumerTag");

			logger.Verify(x => x.Trace(It.Is<string>(s => s == "RabbitMqWrapper.StopMessageConsumer"),
							  It.Is<LogItem>(li => li.Key == "Event" && li.Value() == "Message handler unsubscribed from queue"),
							  It.Is<LogItem>(li => li.Key == "QueueName"),
							  It.Is<LogItem>(li => li.Key == "ConsumerTag" && li.Value() == "TestConsumerTag")), Times.Once);
		}

		[TestMethod]
		public void PublishMessage_WhenCalledWithDefaultTopic_CallsBasicPublish()
		{
			var dateTimeProvider = new Mock<IDateTimeProvider>();
			var logger = new Mock<ILogger>();
			var configurationManager = new Mock<IConfigurationManager>();
		    configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");
		    
            var model = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			connection.Setup(x => x.CreateModel()).Returns(model.Object);
			var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

			var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

			var parameters = new Message<object, object> {
				Uuid = Guid.NewGuid(),
				Source = "test",
				LastModifiedBy = "test",
				Method = "InvokeTest",
				Need = new object()
			};

			rabbitMqWrapper.Start();
			rabbitMqWrapper.PublishMessage(parameters);

			model.Verify(x => x.BasicPublish(It.IsAny<string>(), "A.*", It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
		}

        [TestMethod]
        public void MessageCount_WhenCalled_WithExistentQueueName_ItWillReturnsInt_10()
        {
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var logger = new Mock<ILogger>();
            var configurationManager = new Mock<IConfigurationManager>();
            configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns("amqp://test:test@localhost:5672");            

            var model = new Mock<IModel>();
            model.Setup(x => x.MessageCount("Test")).Returns(10);
            var connection = new Mock<IConnection>();
            connection.Setup(x => x.CreateModel()).Returns(model.Object);
            var connectionManager = new Mock<IRabbitMqConnectionManager>();
            connectionManager.Setup(x => x.Connection).Returns(connection.Object);

            var queueName = "Test";

            IQueueWrapper rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);

            rabbitMqWrapper.Start();

            uint result = rabbitMqWrapper.MessageCount(queueName);
            Assert.AreEqual(10u, result);          
        }

        [TestMethod]
        [ExpectedException(typeof(RabbitMqWrapperException))]
        public void MessageCount_WhenCalled_WithoutModel_RaisesException()
        {
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            var logger = new Mock<ILogger>();
            var configurationManager = new Mock<IConfigurationManager>();
            configurationManager.Setup(x => x.Get<string>("QueueUri")).Returns((string)null);            

            var connectionManager = new Mock<IRabbitMqConnectionManager>();
            
            var rabbitMqWrapper = new RabbitMqWrapper(dateTimeProvider.Object, logger.Object, configurationManager.Object, connectionManager.Object, _metrics);
            var queueName = "Test";

            uint result = rabbitMqWrapper.MessageCount(queueName);
            Assert.AreEqual(1u, result);
        }
    }    
}