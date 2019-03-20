namespace Rook.Framework.Core.Monitoring
{
    /// <summary>
    /// Reasons that a message can be discarded
    /// </summary>
    public enum DiscardedMessageReason
    {
        /// <summary>
        /// The body on the message was null or contained no data
        /// </summary>
        MissingMessageBody,

        /// <summary>
        /// The message could not be deserialised to a MethodInspector object
        /// </summary>
        MethodDeserialisationError,

        /// <summary>
        /// The message was determined to be a duplicate
        /// </summary>
        Duplicate,

        /// <summary>
        /// There was no handler in the microservice 
        /// for the Method on the message
        /// </summary>
        NoHandler,
        
        /// <summary>
        /// The message could not be deserialised to a Message object
        /// </summary>
        MessageDeserialisationError,

        /// <summary>
        /// The AcceptanceBehaviour property of the message
        /// handler indicated that the message was not in 
        /// the correct state for processing
        /// </summary>
        AcceptanceBehaviourPrecondition,

        /// <summary>
        /// The Message had no Need
        /// </summary>
        MissingNeed
    }
}