using System;
using Rook.Framework.Core.Application.MessageHandlers;

namespace Rook.Framework.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public class HandlerAttribute : Attribute
	{
		public string Method { get; }
        public AcceptanceBehaviour AcceptanceBehaviour { get; set; } = AcceptanceBehaviour.Always;

        [Obsolete("Implement " + nameof(IMessageHandler2<object>) + " and return the intended CompletionState instead",true)]
        public CompletionBehaviour CompletionBehaviour { get; set; }

	    public ErrorsBehaviour ErrorsBehaviour { get; set; } = ErrorsBehaviour.AlwaysAccept;

		public HandlerAttribute(string method)
		{
			Method = method;
		}
	}
}
