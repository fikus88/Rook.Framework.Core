using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rook.Framework.Core.Common;
using IHealthCheck = Rook.Framework.Core.Health.IHealthCheck;

namespace Rook.Framework.Core.Application.Bus
{
    public class RabbitMqHealthCheck : IHealthCheck, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
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
	        return CheckHealth();
        }

        private bool CheckHealth()
        {
	        var isHealthy = _connectionManager.Connection?.IsOpen ?? false;

	        if (!isHealthy)
	        {
		        var logItems = new List<LogItem> {new LogItem("Result", "Failed")};
		        logItems.AddRange(
			        _connectionManager.Connection?.ShutdownReport.Select(x => new LogItem("Exception", x.Exception.ToString)) ??
			        new List<LogItem>());

		        _logger.Error($"{nameof(RabbitMqHealthCheck)}.{nameof(IsHealthy)}",
			        logItems.ToArray());
	        }

	        return isHealthy;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
	        return Task.FromResult(CheckHealth() ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
        }
    }
}
