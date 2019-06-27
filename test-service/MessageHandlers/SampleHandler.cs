using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;

namespace test_service.MessageHandlers
{
	[Handler("SampleHandler",
		AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution,
		ErrorsBehaviour = ErrorsBehaviour.RejectIfErrorsExist)]
	public class SampleHandler : IMessageHandler2<object, object>
	{
		public CompletionAction Handle(Message<object, object> message)
		{
			message.Solution = new[] { new object() };
			return CompletionAction.Republish;
		}
	}
}