using System.Collections.Generic;
using KafkaNet.Protocol;

namespace Rook.Framework.Core.AnalyticsPump
{
    /// <summary>
    /// Part of Analytics Pump; this should not be implemented outside of MicroService.Core
    /// </summary>
    public interface IProducerWrapper
    {
        /// <summary>
        /// Part of Analytics Pump; this should not be implemented outside of MicroService.Core
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="messages"></param>
        List<ProduceResponse> SendMessageAsync(string topic, Message[] messages);
    }
}