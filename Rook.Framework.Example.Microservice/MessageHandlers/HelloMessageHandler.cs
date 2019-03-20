using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;

namespace Rook.Framework.Example.Microservice.MessageHandlers
{
    [Handler("Hello", AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution)]
    public class HelloMessageHandler : IMessageHandler2<object, string>
    {
        public CompletionAction Handle(Message<object, string> message)
        {
            message.Solution = new[] { "Hi there!" };
            return CompletionAction.Republish;
        }
    }
}
