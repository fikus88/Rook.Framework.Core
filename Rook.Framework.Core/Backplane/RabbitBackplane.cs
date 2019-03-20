using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Tasks;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;

namespace Rook.Framework.Core.Backplane
{
    internal class RabbitBackplane : IBackplane, IStartStoppable
    {
        private IModel model;
        private readonly string exchangeName;
        private readonly IBackplaneMessageHandler _backplaneMessageHandler;
        private readonly ChannelCache _channelCache;

        public RabbitBackplane(
            ILogger logger,
            IRabbitMqConnectionManager rabbitMqConnectionManager,
            IBackplaneMessageHandler backplaneMessageHandler,
            IBackplaneMetrics backplaneMetrics)
        {
            exchangeName = $"{ServiceInfo.Name}{ServiceInfo.MajorVersion}";
            _backplaneMessageHandler = backplaneMessageHandler;

            _channelCache = new ChannelCache(rabbitMqConnectionManager, logger, exchangeName, ExchangeType.Fanout, false, backplaneMetrics.RecordNewBackplaneChannel);
        }

        public void Send<T>(T data)
        {
            ObjectWrapper<T> wr = new ObjectWrapper<T>
            {
                Type = typeof(T).GUID,
                TypeName = typeof(T).Name,
                Data = data
            };
            
            var publishModel = _channelCache.GetNextAvailableChannel();

            publishModel.BasicPublish(exchangeName, "*", body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(wr)));

            _channelCache.ReleaseChannel(publishModel);
        }

        public StartupPriority StartupPriority { get; } = StartupPriority.Normal;
        public void Stop()
        {
            _channelCache.Dispose();
        }

        public void Start()
        {
            if (model != null) return;
            model = _channelCache.CreateChannel();
            
            string queueName = model.QueueDeclare().QueueName;
            model.QueueBind(queueName, exchangeName, "*", null);

            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += OnMessageReceived;

            model.BasicConsume(queueName, false, consumer);
        }

        private Task OnMessageReceived(object sender, BasicDeliverEventArgs eventDetails)
        {
            if (eventDetails.Body == null || eventDetails.Body.Length < 1)
            {
                model.BasicReject(eventDetails.DeliveryTag, false);
                return Task.CompletedTask;
            }

            try
            {
                _backplaneMessageHandler.Handle(eventDetails.Body);
            }
            finally
            {
                model.BasicAck(eventDetails.DeliveryTag, false);
            }

            return Task.CompletedTask;
        }
    }
}
