using Prometheus;
using Prometheus.Advanced;
using Prometheus.Advanced.DataContracts;
using System;
using System.Linq;
using System.Net;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;

namespace Rook.Framework.Core.Monitoring
{
    /// <summary>
    /// Prometheus /metrics HTTP endpoint
    /// Publishes Prometheus metrics
    /// </summary>
    public class MetricsActivityHandler : IActivityHandler
    {
        private readonly ICollectorRegistry _registry;
        private readonly ILogger _logger;

        public MetricsActivityHandler(ICollectorRegistry registry,
            ILogger logger)
        {
            _registry = registry;
            _logger = logger;
        }

        public void Handle(IHttpRequest request, IHttpResponse response)
        {
            // Largely cribbed from https://github.com/prometheus-net/prometheus-net/blob/master/Prometheus.NetStandard/MetricServer.cs

            MetricFamily[] metrics;

            try
            {
                
                metrics = _registry.CollectAll().ToArray();

                _logger.Trace($"{nameof(MetricsActivityHandler)}.{nameof(Handle)}",
                    new LogItem("Event", "CollectAll result"),
                    new LogItem("MetricsCount", metrics.Length));

                var acceptHeader = request.RequestHeader["Accept"];
                var acceptHeaders = acceptHeader?.Split(',');

                var contentType = ScrapeHandler.GetContentType(acceptHeaders);
                

                response.HttpStatusCode = HttpStatusCode.OK;

                response.HttpContent = new MetricsHttpContent(_logger, metrics, contentType);                
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(MetricsActivityHandler)}.{nameof(Handle)}",
                        new LogItem("Event", "ScrapeFailed"),
                        new LogItem("Exception", ex.Message),
                        new LogItem("StackTrace", ex.StackTrace));

                response.HttpStatusCode = HttpStatusCode.ServiceUnavailable;
                response.SetStringContent(ex.Message);
            }
        }

        public dynamic ExampleRequestDocument { get; } = null;
        public dynamic ExampleResponseDocument { get; } = null;
    }
}
