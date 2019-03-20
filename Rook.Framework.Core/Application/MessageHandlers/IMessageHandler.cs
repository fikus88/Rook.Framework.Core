using System;
using Rook.Framework.Core.Application.Message;

namespace Rook.Framework.Core.Application.MessageHandlers
{
    [Obsolete("Use " + nameof(IMessageHandler2<object>) + " instead - or wait until " + nameof(IMessageHandler2<object>) + " becomes " + nameof(IMessageHandler<object>),true)]
    public interface IMessageHandler<TNeed> : IMessageHandler<TNeed, object> { }

    [Obsolete("Use " + nameof(IMessageHandler2<object>) + " instead - or wait until " + nameof(IMessageHandler2<object>) + " becomes " + nameof(IMessageHandler<object>),true)]
    public interface IMessageHandler<TNeed, TSolution>
    {
        void Handle(Message<TNeed, TSolution> message);
    }

    public interface IMessageHandler2<TNeed> : IMessageHandler2<TNeed, object> { }

    public interface IMessageHandler2<TNeed, TSolution>
    {
        CompletionAction Handle(Message<TNeed, TSolution> message);
    }
    
    /// <summary>
    /// Actions to take after returning from the Handle method of a MessageHandler
    /// </summary>
    public enum CompletionAction
    {
        [Obsolete("This preserves the behaviour of CompletionBehaviour, which is also Deprecated",true)]
        Legacy = 0,
        /// <summary>
        /// Do nothing after returning from the Handle method
        /// </summary>
        DoNothing = 1,
        /// <summary>
        /// Republish the message after returning from the Handle method
        /// </summary>
        Republish = 2
    }
}
