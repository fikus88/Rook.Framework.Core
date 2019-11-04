using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rook.Framework.Core.AmazonKinesisFirehose;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.LambdaDataPump;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;

namespace Rook.Framework.Core.Application.Bus
{
	public sealed class RabbitMqWrapper : IQueueWrapper, IDisposable, IStartStoppable
	{
		internal readonly string QueueName;
		private readonly bool _autoAck;
		private readonly bool _durable;

		internal readonly ILogger Logger;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly IServiceMetrics _serviceMetrics;
		private readonly ushort _maximumConcurrency;
		private readonly IAmazonFirehoseProducer _amazonFirehoseProducer;
		private readonly string _amazonKinesisStreamName;
		private readonly ILambdaDataPump _lambdaDataPump;
		private readonly string _dataPumpLambdaName;


		private string SelectedRoutingKey { get; set; }
		internal IModel Model { get; set; }

		private readonly ChannelCache _channelCache;

		public RabbitMqWrapper(
			IDateTimeProvider dateTimeProvider,
			ILogger logger,
			IConfigurationManager configurationManager,
			IRabbitMqConnectionManager connectionManager,
			IServiceMetrics serviceMetrics)
		{
			Logger = logger;
			_dateTimeProvider = dateTimeProvider;
			_serviceMetrics = serviceMetrics;

			QueueName = ServiceInfo.QueueName;

			// AutoAck is the opposite of AcknowledgeAfterProcessing
			// AcknowledgeAfterProcessing will ack the message after it's been processed,
			// while AutoAck (AcknowledgeAfterProcessing=False) will ack the message after it's
			// been pulled from the queue.
			_autoAck = !configurationManager.Get<bool>("AcknowledgeAfterProcessing", true);

			_durable = configurationManager.Get<bool>("QueueIsDurable", true);



			try
			{
				_amazonKinesisStreamName = configurationManager.Get<string>("MessageKinesisStream");
			}
			catch
			{
				_amazonKinesisStreamName = null;
			}


			try
			{
				_dataPumpLambdaName = configurationManager.Get<string>("DataPumpLambdaName");
			}
			catch
			{
				_dataPumpLambdaName = null;
			}
		

			if (!string.IsNullOrEmpty(_amazonKinesisStreamName))
				_amazonFirehoseProducer = new AmazonFirehoseProducer(logger, configurationManager);
			
			if (!string.IsNullOrEmpty(_dataPumpLambdaName))
				_lambdaDataPump = new LambdaDataPump.LambdaDataPump(logger, _dataPumpLambdaName);
			

			_maximumConcurrency = configurationManager.Get<ushort>("MaximumConcurrency", 0);

			_channelCache = new ChannelCache(connectionManager, Logger, QueueConstants.ExchangeName, ExchangeType.Topic,
				true, () => _serviceMetrics.RecordNewMainChannel());
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (disposing)
			{
				_channelCache.Dispose();

				Model?.Close();
				Model?.Dispose();
				Model = null;
			}
		}

		public StartupPriority StartupPriority { get; } = StartupPriority.High;

		public void Start()
		{
			Start(QueueConstants.DefaultRoutingKey);
		}

		public void Start(string topic)
		{
			SelectedRoutingKey = topic;

			if (Model != null) return;

			Model = _channelCache.CreateChannel();

			if (_maximumConcurrency > 0)
			{
				Model.BasicQos(0, _maximumConcurrency, false);
			}

			if (!string.IsNullOrWhiteSpace(QueueName))
			{
				Logger.Info($"Declaring & Binding to Queue {QueueName}...");
				Model.QueueDeclare(QueueName, _durable, false, false);
				Model.QueueBind(QueueName, QueueConstants.ExchangeName, SelectedRoutingKey);
			}
		}

		public void Stop() => Dispose();

		public string StartMessageConsumer(AsyncEventHandler<BasicDeliverEventArgs> consumeMessageHandler)
		{
			if (Model == null)
				throw new RabbitMqWrapperException(
					$"{nameof(RabbitMqWrapper)}.{nameof(Start)} must be run before calling {nameof(StartMessageConsumer)}");

			var consumer = new AsyncEventingBasicConsumer(Model);
			consumer.Received += async (sender, @event) => { await consumeMessageHandler(sender, @event); };

			var consumerTag = Model.BasicConsume(QueueName, _autoAck, consumer);

			Logger.Trace($"{nameof(RabbitMqWrapper)}.{nameof(StartMessageConsumer)}",
				new LogItem("Event", "Message handler subscribed to queue"),
				new LogItem("QueueName", QueueName),
				new LogItem("ConsumerTag", consumerTag),
				new LogItem("HandlerMethod", () => consumeMessageHandler.GetMethodInfo().Name));
			return consumerTag;
		}

		public void StopMessageConsumer(string consumerTag)
		{
			if (Model == null)
			{
				Logger.Warn(
					$"{nameof(RabbitMqWrapper)}.{nameof(Start)} must be run before calling {nameof(StopMessageConsumer)}");
				return;
			}

			if (!string.IsNullOrWhiteSpace(consumerTag))
			{
				lock (Model)
					Model.BasicCancel(consumerTag);
				Logger.Trace($"{nameof(RabbitMqWrapper)}.{nameof(StopMessageConsumer)}",
					new LogItem("Event", "Message handler unsubscribed from queue"),
					new LogItem("QueueName", QueueName),
					new LogItem("ConsumerTag", consumerTag));
			}
		}

		public void RejectMessage(BasicDeliverEventArgs eventDetails)
		{
			if (!_autoAck)
				lock (Model)
					Model.BasicReject(eventDetails.DeliveryTag, false);
		}

		public void AcknowledgeMessage(BasicDeliverEventArgs eventDetails)
		{
			if (!_autoAck)
				lock (Model)
					Model.BasicAck(eventDetails.DeliveryTag, false);
		}

		public void PublishMessage<TNeed, TSolution>(Message<TNeed, TSolution> message, Guid uuid = default)
		{
			if (message.Source == null)
				message.Source = ServiceInfo.Name;

			if (uuid == Guid.Empty) uuid = Guid.NewGuid();

			message.LastModifiedBy = ServiceInfo.Name;
			message.LastModifiedTime = _dateTimeProvider.UtcNow;
			message.Uuid = uuid;
			message.PublishedTime = _dateTimeProvider.UtcNow;

			string serializedMessage = JsonConvert.SerializeObject(message,
				new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()});

			IModel model = _channelCache.GetNextAvailableChannel();

			model.BasicPublish(QueueConstants.ExchangeName, SelectedRoutingKey, true, null,
				Encoding.UTF8.GetBytes(serializedMessage));
			

			if (!string.IsNullOrEmpty(_dataPumpLambdaName))
				Task.Run(() => _lambdaDataPump.InvokeLambdaAsync(serializedMessage)).ConfigureAwait(false);

			_channelCache.ReleaseChannel(model);

			Logger.Trace($"{nameof(RabbitMqWrapper)}.{nameof(PublishMessage)}",
				new LogItem("Event", "Message published"),
				new LogItem("Message", serializedMessage),
				new LogItem("Topic", SelectedRoutingKey),
				new LogItem("MessageId", uuid.ToString));

			_serviceMetrics.RecordPublishedMessage();
		}

		public uint MessageCount(string queueName)
		{
			if (Model == null)
				throw new RabbitMqWrapperException(
					$"{nameof(RabbitMqWrapper)}.{nameof(Start)} must be run before calling {nameof(PublishMessage)}");

			return Model.MessageCount(queueName);
		}
	}
}