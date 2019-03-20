using System;

namespace Rook.Framework.Core.Attributes
{
    /// <summary>
    /// Handler attribute which only accepts messages without solutions and without errors, and which automatically republishes a message on completion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class QueryHandlerAttribute : HandlerAttribute
    {
        public QueryHandlerAttribute(string method) : base(method)
        {
            AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution;
            ErrorsBehaviour = ErrorsBehaviour.RejectIfErrorsExist;            
        }
    }
}