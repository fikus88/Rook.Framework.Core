using System;

namespace Rook.Framework.Core.Attributes
{
    /// <summary>
    /// Handler attribute which only accepts messages with solutions and without errors, and which does not automatically republish a message on completion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CommandHandlerAttribute : HandlerAttribute
    {
        public CommandHandlerAttribute(string method) : base(method)
        {
            AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithSolution;
            ErrorsBehaviour = ErrorsBehaviour.RejectIfErrorsExist;
        }
    }
}