using System;

namespace Rook.Framework.Core.Exceptions
{
    public sealed class ExternalCallFailedException : Exception
    {
        public ExternalCallFailedException()
        {
        }

        public ExternalCallFailedException(string message) : base(message)
        {
        }

        public ExternalCallFailedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}