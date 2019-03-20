using System;

namespace Rook.Framework.Core.Attributes
{
    /// <summary>
    /// Handler attribute which only accepts messages with errors, whether or not a solution exists, and does not automatically republish the message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ErrorHandlerAttribute : HandlerAttribute
    {
        public ErrorHandlerAttribute(string method) : base(method)
        {
            AcceptanceBehaviour = AcceptanceBehaviour.Always;
            ErrorsBehaviour = ErrorsBehaviour.AcceptOnlyIfErrorsExist;
        }
    }
}