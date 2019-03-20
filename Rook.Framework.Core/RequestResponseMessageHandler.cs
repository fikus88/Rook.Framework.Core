using System.Linq;
using Newtonsoft.Json;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core
{
	[Handler("*")]
	public class RequestResponseMessageHandler<TNeed, TSolution> : IMessageHandler2<TNeed, TSolution>
	{
        private readonly IBackplane backplane;
	    private readonly ILogger _logger;
		private readonly IRequestStore requestStore;

		public RequestResponseMessageHandler(IBackplane backplane, ILogger logger, IRequestStore requestStore)
	    {
	        this.backplane = backplane;
	        _logger = logger;
		    this.requestStore = requestStore;
	    }

		public CompletionAction Handle(Message<TNeed, TSolution> message)
		{
		    // If Solution == null and there are no errors, we have nothing to store.
			if (message.Solution == null && !message.Errors.Any()) return CompletionAction.DoNothing;

			if (requestStore.Methods.Contains(message.Method))
			{
				// We get here if:
				//   - (there is a solution on the message, or
				//   - there are errors on the message)
				// AND the message.Method has been asked for

				MessageWrapper mw = new MessageWrapper {
					Uuid = message.Uuid,
					SolutionJson = JsonConvert.SerializeObject(message.Solution),
					ErrorsJson = JsonConvert.SerializeObject(message.Errors)
				};

				_logger.Info($"{nameof(RequestResponseMessageHandler<TNeed, TSolution>)}.{nameof(Handle)}",
					new LogItem("Event", "Response received"),
					new LogItem("MessageId", message.Uuid.ToString),
					new LogItem("MessageContainsSolution", ((message.Solution?.Length ?? 0) > 0).ToString()),
					new LogItem("MessageContainerErrors", ((message.Errors?.Count ?? 0) > 0).ToString()));

				// When there is a solution to a message we're interested in, 
				// send it on the private exchange
				if (message.Solution != null)
					mw.FirstOrDefaultJson = JsonConvert.SerializeObject(message.Solution.FirstOrDefault());

				backplane.Send(mw);
			}
			return CompletionAction.DoNothing;
		}
	}
}