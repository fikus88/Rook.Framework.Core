using Microlise.MicroService.Core.Application.Bus;
using Microlise.MicroService.Core.Application.Message;
using Microlise.MicroService.Core.Application.MessageHandlers;
using Microlise.MicroService.Core.Attributes;

namespace Microlise.Example.Microservice.MessageHandlers
{
	[Handler("Hello")]
	public class HelloMessageHandler : IMessageHandler<object, string>
	{
		private readonly IQueueWrapper queueWrapper;

		public HelloMessageHandler(IQueueWrapper queueWrapper)
		{
			this.queueWrapper = queueWrapper;
		}

		public void Handle(Message<object, string> message)
		{
			message.Solution = "Hi there!";
			queueWrapper.PublishMessage(message);
		}
	}
}
