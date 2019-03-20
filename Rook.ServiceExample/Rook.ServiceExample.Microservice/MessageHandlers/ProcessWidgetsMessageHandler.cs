using Microlise.Example.Microservice.Objects;
using Microlise.MicroService.Core.Application.Bus;
using Microlise.MicroService.Core.Application.Message;
using Microlise.MicroService.Core.Application.MessageHandlers;
using Microlise.MicroService.Core.Attributes;
using Microlise.MicroService.Core.Common;
using Microlise.MicroService.Core.Data;

namespace Microlise.Example.Microservice.MessageHandlers
{
	[Handler("ProcessWidgets")]
	public class ProcessWidgetsMessageHandler : IMessageHandler<Widget[], string[]>
	{
		private readonly IQueueWrapper queueWrapper;
		private readonly ILogger logger;
		private readonly IDateTimeProvider dateTimeProvider;
		private readonly IMongoStore datastore;

		public ProcessWidgetsMessageHandler(IQueueWrapper queueWrapper,
			ILogger logger,
			IDateTimeProvider dateTimeProvider,
			IMongoStore datastore)
		{
			this.queueWrapper = queueWrapper;
			this.logger = logger;
			this.dateTimeProvider = dateTimeProvider;
			this.datastore = datastore;
		}

		public void Handle(Message<Widget[], string[]> message)
		{
			Widget[] widgetsToProcess = message.Need;
			message.Solution = new string[widgetsToProcess.Length];
			for (int n = 0; n < widgetsToProcess.Length; n++)
			{
				Widget widget = widgetsToProcess[n];
				message.Solution[n] = widget.Process();
				datastore.Put(widget);
			}
			queueWrapper.PublishMessage(message);
		}
	}
}