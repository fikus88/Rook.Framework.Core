using Prometheus;

namespace Rook.Framework.Core.Monitoring
{
    public class BackplaneMetrics : IBackplaneMetrics
    {
        private readonly Histogram _messageHandlerDurationHistogram = Metrics.CreateHistogram(
               $"{ServiceMetrics.MetricsPrefix}_backplane_message_handler_duration_seconds",
               "Time taken for a message to be processed by a backplane handler",
               labelNames: new[] { "handler" });


        public void RecordProcessedMessage(string handlerName, double elapsedMilliseconds)
        {
            _messageHandlerDurationHistogram.Labels(handlerName).Observe(elapsedMilliseconds / 1000);
        }


        private readonly Counter _rabbitMqOpenBackplaneChannels = Metrics.CreateCounter(
            $"{ServiceMetrics.MetricsPrefix}_open_backplane_channels",
            "Number of channels open on the backplane exchange");

        public void RecordNewBackplaneChannel()
        {
            _rabbitMqOpenBackplaneChannels.Inc();
        }
    }
}
