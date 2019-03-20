using System;
using System.IO;
using RabbitMQ.Client;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Application.Bus
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        IConnection Connection { get; }
    }

    internal class RabbitMqConnectionManager : IRabbitMqConnectionManager
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly string _queueUri;

        public IConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connectionFactory.Uri = new Uri(_queueUri);
                    _connectionFactory.RequestedHeartbeat = QueueConstants.QueueHeartBeatTimeOutInSeconds;
                    if (_connectionFactory is ConnectionFactory factory)
                    {
                        factory.DispatchConsumersAsync = true;
                    }

                    _connection = _connectionFactory.CreateConnection();
                }

                return _connection;
            }
        }

        public RabbitMqConnectionManager(IConnectionFactory connectionFactory, IConfigurationManager configurationManager)
        {
            _connectionFactory = connectionFactory;

            _queueUri = configurationManager.Get<string>("QueueUri");
            if (string.IsNullOrEmpty(_queueUri))
                throw new RabbitMqWrapperException($"{nameof(configurationManager)}.{nameof(IConfigurationManager.Get)}.{nameof(_queueUri)} cannot be empty.");
        }

        public void Dispose()
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();

                _connection = null;
            }
            catch (EndOfStreamException)
            {
            }
        }
    }
}