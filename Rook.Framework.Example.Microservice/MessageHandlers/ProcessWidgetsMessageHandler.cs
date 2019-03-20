using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.Common;
using Rook.Framework.Example.Microservice.Objects;

namespace Rook.Framework.Example.Microservice.MessageHandlers
{
	[Handler("ProcessWidgets", AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution)]
	public class ProcessWidgetsMessageHandler : IMessageHandler2<Widget[], string>
	{
		
		
		private readonly IQueueWrapper queueWrapper;
		private readonly ILogger logger;
		private readonly IDateTimeProvider dateTimeProvider;

		public ProcessWidgetsMessageHandler(IQueueWrapper queueWrapper,
			ILogger logger,
			IDateTimeProvider dateTimeProvider)
		{
			this.queueWrapper = queueWrapper;
			this.logger = logger;
			this.dateTimeProvider = dateTimeProvider;
		}

		public CompletionAction Handle(Message<Widget[], string> message)
		{
			Widget[] widgetsToProcess = message.Need;
			message.Solution = new string[widgetsToProcess.Length];
			for (int n = 0; n < widgetsToProcess.Length; n++)
			{
				Widget widget = widgetsToProcess[n];
				message.Solution[n] = widget.Process();
				WidgetStore.Put(widget);
			}
			queueWrapper.PublishMessage(message);

            return CompletionAction.DoNothing;
		}
	}
}