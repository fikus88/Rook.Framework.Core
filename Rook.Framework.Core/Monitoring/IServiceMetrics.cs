namespace Rook.Framework.Core.Monitoring
{
    /// <summary>
    /// Exposes service metrics
    /// </summary>
    public interface IServiceMetrics
    {
        /// <summary>
        /// Increments the discared message counter
        /// </summary>
        void RecordDiscardedMessage(DiscardedMessageReason reason);

        /// <summary>
        /// Increments the published messages counter
        /// </summary>
        void RecordPublishedMessage();

        /// <summary>
        /// Record processing time for the message handler
        /// </summary>
        /// <param name="handlerName">Name of the message handler</param>
        /// <param name="elapsedMilliseconds">Processing time in milliseconds</param>
        void RecordProcessedMessage(string handlerName, double elapsedMilliseconds);

        /// <summary>
        /// Records a new channel being opened on the main exchange
        /// </summary>
        void RecordNewMainChannel();
    }
}
