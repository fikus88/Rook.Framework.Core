using System;
using RabbitMQ.Client;

namespace Rook.Framework.Core.Application.Bus
{
    internal class ChannelCacheEntry
    {
        public ChannelCacheEntry(IModel channel)
        {
            Channel = channel;
        }

        public IModel Channel { get; set; }
        public DateTime? LastUsed { get; set; }
    }
}