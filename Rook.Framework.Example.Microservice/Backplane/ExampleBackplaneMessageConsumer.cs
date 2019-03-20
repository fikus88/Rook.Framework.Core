using StructureMap;
using System;
using Rook.Framework.Core.Backplane;

namespace Rook.Framework.Example.Microservice.Backplane
{
    class ExampleBackplaneMessageConsumer : BackplaneConsumer<BackplaneMessage>
    {
        /// <summary>
        /// This is the "handler" method for backplane messages.
        /// Logic for handling incoming messages should go in here.
        /// </summary>
        /// <param name="value"></param>
        public override void Consume(BackplaneMessage value)
        {
            Console.WriteLine(value.Message);
        }
    }

    /// <summary>
    /// Exxample class for publishing a message to the backplane.
    /// </summary>
    class BackplaneMessagePublisher
    {
        public void PublishBackplaneMessage(IBackplane backplane)
        {
            backplane.Send(new BackplaneMessage() { Message = "backplane message" });
        }
    }

    /// <summary>
    /// The message to publish to the backplane.
    /// You must use a complex type (i.e. a class). Primitive types (i.e. strings) CANNOT be used as message types.
    /// </summary>
    public class BackplaneMessage
    {
        public string Message { get; set; }
    }

    /// <summary>
    /// The backplane uses the IoC container to construct instances of the consumer and to work out the type of the message.
    /// For this reason, you must register both the consumer AND the message type with the IoC container.
    /// </summary>
    public class ExampleBackplaneConsumerRegistry : Registry
    {
        public ExampleBackplaneConsumerRegistry()
        {
            For<BackplaneMessage>().Use<BackplaneMessage>();
            For<IBackplaneConsumer>().Add<ExampleBackplaneMessageConsumer>();
        }
    }
}
