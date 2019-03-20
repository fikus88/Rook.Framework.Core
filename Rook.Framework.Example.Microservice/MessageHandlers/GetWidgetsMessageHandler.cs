using System.Linq;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.Common;
using Rook.Framework.Example.Microservice.Objects;

namespace Rook.Framework.Example.Microservice.MessageHandlers
{

    [Handler("GetWidgets", AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution)]
    public class GetWidgetsMessageHandler : IMessageHandler2<int, Widget>
    {
        private readonly IQueueWrapper queueWrapper;
        private readonly ILogger logger;
        private readonly IDateTimeProvider dateTimeProvider;
        
        public GetWidgetsMessageHandler(IQueueWrapper queueWrapper,
            ILogger logger,
            IDateTimeProvider dateTimeProvider)
        {
            this.queueWrapper = queueWrapper;
            this.logger = logger;
            this.dateTimeProvider = dateTimeProvider;
            
        }

        public CompletionAction Handle(Message<int, Widget> message)
        {
            int numberOfWidgets = message.Need;

            message.Solution = WidgetStore.Get().Take(numberOfWidgets).ToArray();

            queueWrapper.PublishMessage(message);

            return CompletionAction.DoNothing;
        }
    }
}