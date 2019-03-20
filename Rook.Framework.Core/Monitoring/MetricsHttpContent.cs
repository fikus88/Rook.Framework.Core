using Prometheus;
using Prometheus.Advanced.DataContracts;
using System;
using System.Collections.Generic;
using System.IO;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;

namespace Rook.Framework.Core.Monitoring
{
    public class MetricsHttpContent : IHttpContent
    {
        private readonly ILogger _logger;
        private readonly MetricFamily[] _metrics;
        private readonly string _contentType;

        public IEnumerable<KeyValuePair<string, string>> Headers { get; }

        public MetricsHttpContent(ILogger logger,
            MetricFamily[] metrics, string contentType)
        {
            _logger = logger;
            _contentType = contentType;
            _metrics = metrics;

            Headers = new[]
            {
                new KeyValuePair<string, string>("Content-Type", _contentType )
            };
        }

        public void WriteToStream(Stream stream)
        {
            _logger.Trace($"{nameof(MetricsHttpContent)}.{nameof(WriteToStream)}",
                       new LogItem("Event", "ProcessScrapeRequest"));

            try
            {
                ScrapeHandler.ProcessScrapeRequest(_metrics, _contentType, stream);
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(MetricsHttpContent)}.{nameof(WriteToStream)}",
                        new LogItem("Event", "ProcessScrapeRequest failed"),
                        new LogItem("Exception", ex.Message),
                        new LogItem("StackTrace", ex.StackTrace));
            }
        }
    }
}
