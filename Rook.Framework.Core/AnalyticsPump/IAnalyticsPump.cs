namespace Rook.Framework.Core.AnalyticsPump
{
    /// <summary>
    /// Forwards messages to the Analytics Pump (via Kafka)
    /// </summary>
    public interface IAnalyticsPump
    {
        /// <summary>
        /// Forwards a json message to the Analytics Pump (via Kafka)
        /// </summary>
        void ForwardMessageToPump(string messageJson, byte[] key = null);
    }
}