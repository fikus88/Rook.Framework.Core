using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Application.Bus
{
    public class ChannelCache : IDisposable
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly ILogger _logger;
        private readonly string _exchange;
        private readonly string _type;
        private readonly bool _durable;
        private readonly Action _incremenetChannelCountMetric;
        readonly List<ChannelCacheEntry> _channelCache = new List<ChannelCacheEntry>();

        public ChannelCache(IRabbitMqConnectionManager connectionManager, ILogger logger, string exchange, string type, bool durable, Action incremenetChannelCountMetric)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _exchange = exchange;
            _type = type;
            _durable = durable;
            _incremenetChannelCountMetric = incremenetChannelCountMetric;
        }

        public IModel GetNextAvailableChannel()
        {
            lock (_channelCache)
            {
                ChannelCacheEntry cacheEntry = _channelCache.FirstOrDefault(c => c.LastUsed == null || (DateTime.UtcNow - c.LastUsed.Value) > TimeSpan.FromSeconds(10));
                if (cacheEntry == null)
                {
                    cacheEntry = new ChannelCacheEntry(CreateChannel());
                    _channelCache.Add(cacheEntry);
                }
                cacheEntry.LastUsed = DateTime.UtcNow;
                return cacheEntry.Channel;
            }
        }

        public void ReleaseChannel(IModel model)
        {
            lock(_channelCache)
                _channelCache.First(c => ReferenceEquals(c.Channel, model)).LastUsed = null;
        }

        public IModel CreateChannel()
        {
            var model = _connectionManager.Connection.CreateModel();
            model.CallbackException += (sender, args) => { _logger.Warn($"{nameof(model)}.{nameof(model.CallbackException)}", new LogItem("Exception", args.Exception.ToString)); };
            model.ExchangeDeclare(_exchange, _type, _durable, false, null);

            _incremenetChannelCountMetric();

            return model;
        }

        public void Dispose()
        {
            foreach (ChannelCacheEntry cacheEntry in _channelCache)
            {
                cacheEntry.Channel?.Close();
                cacheEntry.Channel?.Dispose();
            }
        }
    }
}