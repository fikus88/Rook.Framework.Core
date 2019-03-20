using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.TestUtils
{
    public static class RabbitMqWrapperTestUtils
    {
        public static void DeleteQueue(this RabbitMqWrapper queueWrapper)
        {
            if (queueWrapper.Model == null) throw new RabbitMqWrapperException($"{nameof(RabbitMqWrapper)}.{nameof(RabbitMqWrapper.Start)} must be run before calling {nameof(DeleteQueue)}");

            queueWrapper.Model.QueueDeleteNoWait(queueWrapper.QueueName, false, false);
            queueWrapper.Logger.Trace($"{nameof(RabbitMqWrapper)}.{nameof(DeleteQueue)}", new LogItem("Event", "Message queue deleted"), new LogItem("QueueName", queueWrapper.QueueName));
        }

        public static void PurgeQueue(this RabbitMqWrapper queueWrapper)
        {
            if (queueWrapper.Model == null) throw new RabbitMqWrapperException($"{nameof(RabbitMqWrapper)}.{nameof(RabbitMqWrapper.Start)} must be run before calling {nameof(PurgeQueue)}");
            queueWrapper.Model.QueuePurge(queueWrapper.QueueName);
            queueWrapper.Logger.Trace($"{nameof(RabbitMqWrapper)}.{nameof(PurgeQueue)}", new LogItem("Event", "Message queue purged"), new LogItem("QueueName", queueWrapper.QueueName));
        }
    }
}