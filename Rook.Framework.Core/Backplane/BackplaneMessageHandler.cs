using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Backplane
{
    internal interface IBackplaneMessageHandler
    {
        Task Handle(byte[] message);
    }

    internal class BackplaneMessageHandler : IBackplaneMessageHandler
    {
        private readonly IContainerFacade _container;
        private readonly IBackplaneMetrics _backplaneMetrics;
        private readonly ILogger _logger;

        public BackplaneMessageHandler(
            IContainerFacade containerFacade,
            IBackplaneMetrics backplaneMetrics,
            ILogger logger)
        {
            _container = containerFacade;
            _backplaneMetrics = backplaneMetrics;
            _logger = logger;
        }

        public Task Handle(byte[] message)
        {
            ObjectHeader header = JsonConvert.DeserializeObject<ObjectHeader>(Encoding.UTF8.GetString(message));

            Type objectType = _container.GetByTypeGuid(header.Type);

            if (objectType == null)
            {
                throw new InvalidOperationException(
                    $"A message was received on the Backplane which had a Type GUID ({header.Type} - {header.TypeName}) that could not be resolved. " +
                    "This can be caused by an incorrect Backplane Consumer, or by two versions of the same service trying to use the Backplane at once.");
            }

            Type wrapperType = typeof(ObjectWrapper<>).MakeGenericType(objectType);

            object v = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message), wrapperType);

            if(TryFindConsumer(header.Type, out var consumer))
            {
                return Task.Run(() => ConsumeMessage(v, consumer));
            }
            else
            {
                throw new InvalidOperationException($"No matching consumer found for type {objectType.Name}");
            }
        }

        private void ConsumeMessage(object message, IBackplaneConsumer consumer)
        {
            var stopwatch = Stopwatch.StartNew();

            consumer.Consume(((dynamic)message).Data);

            stopwatch.Stop();

            _backplaneMetrics.RecordProcessedMessage(consumer.GetType().Name, stopwatch.Elapsed.TotalMilliseconds);
        }

        private bool TryFindConsumer(Guid type, out IBackplaneConsumer consumer)
        {
            consumer = _container.GetAllInstances<IBackplaneConsumer>().FirstOrDefault(x => x.ConsumesType == type);

            return consumer != null;
        }
    }

    internal class ObjectHeader
    {
        public Guid Type;
        public string TypeName { get; set; }
    }

    internal class ObjectWrapper<T> : ObjectHeader
    {
        public T Data;
    }
}
