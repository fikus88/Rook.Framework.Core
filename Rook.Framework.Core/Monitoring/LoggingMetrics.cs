using Prometheus;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Monitoring
{
    public class LoggingMetrics : ILoggingMetrics
    {
        private readonly Counter _logMessageCounter = Metrics.CreateCounter(
           $"{ServiceMetrics.MetricsPrefix}_log_messages",
           "Number of log messages",
           "level");

        public void RecordLogMessage(LogLevel level)
        {
            _logMessageCounter.Labels(level.ToString()).Inc();
        }

    }
}
