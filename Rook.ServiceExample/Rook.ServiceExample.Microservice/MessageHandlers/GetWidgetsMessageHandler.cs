using System.Linq;
using Microlise.Example.Microservice.Objects;
using Microlise.MicroService.Core.Application.Bus;
using Microlise.MicroService.Core.Application.Message;
using Microlise.MicroService.Core.Application.MessageHandlers;
using Microlise.MicroService.Core.Attributes;
using Microlise.MicroService.Core.Common;
using Microlise.MicroService.Core.Data;

namespace Microlise.Example.Microservice.MessageHandlers
{

    [Handler("GetWidgets")]
    public class GetWidgetsMessageHandler : IMessageHandler<int, Widget[]>
    {

        private readonly IQueueWrapper queueWrapper;
        private readonly ILogger logger;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IMongoStore datastore;

        public GetWidgetsMessageHandler(IQueueWrapper queueWrapper,
            ILogger logger,
            IDateTimeProvider dateTimeProvider,
            IMongoStore datastore)
        {
            this.queueWrapper = queueWrapper;
            this.logger = logger;
            this.dateTimeProvider = dateTimeProvider;
            this.datastore = datastore;
        }

        public void Handle(Message<int, Widget[]> message)
        {
            int numberOfWidgets = message.Need;

            message.Solution = datastore.Get<Widget>(w => true).Take(numberOfWidgets).ToArray();

            queueWrapper.PublishMessage(message);
        }
    }
}