using System;

namespace Rook.Framework.Core.Application.Bus
{
    /// <summary>
    /// Exception from RabbitMqWrapper
    /// </summary>
    [Serializable]
    public sealed class RabbitMqWrapperException : Exception
    {
        internal RabbitMqWrapperException(string message) : base(message) { }
    }
}