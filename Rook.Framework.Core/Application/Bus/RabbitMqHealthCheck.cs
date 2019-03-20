using System.Collections.Generic;
using System.Linq;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Health;

namespace Rook.Framework.Core.Application.Bus
{
    public class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly ILogger _logger;
        private readonly IRabbitMqConnectionManager _connectionManager;

        public RabbitMqHealthCheck(ILogger logger, IRabbitMqConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public bool IsHealthy()
        {
            var isHealthy =  _connectionManager.Connection?.IsOpen ?? false;

            if (!isHealthy)
            {
                var logItems = new List<LogItem> {new LogItem("Result", "Failed")};
                logItems.AddRange(_connectionManager.Connection?.ShutdownReport.Select(x => new LogItem("Exception", x.Exception.ToString)) ?? new List<LogItem>());

                _logger.Error($"{nameof(RabbitMqHealthCheck)}.{nameof(IsHealthy)}",
                    logItems.ToArray());
            }

            return isHealthy;
        }
    }
}
