using System;
using RabbitMQ.Client.Events;
using Rook.Framework.Core.Application.Message;

namespace Rook.Framework.Core.Application.Bus
{
    public interface IQueueWrapper
    {
        void PublishMessage<TNeed, TSolution>(Message<TNeed, TSolution> message, Guid uuid = default);
        void Start(string topic = QueueConstants.DefaultRoutingKey);
        string StartMessageConsumer(AsyncEventHandler<BasicDeliverEventArgs> consumeMessageHandler);
        void StopMessageConsumer(string consumerTag);
        void RejectMessage(BasicDeliverEventArgs eventDetails);
        void AcknowledgeMessage(BasicDeliverEventArgs eventDetails);
        uint MessageCount(string queueName);
    }
}
