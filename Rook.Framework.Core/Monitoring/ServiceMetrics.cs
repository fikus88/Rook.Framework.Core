using Prometheus;
using Prometheus.Advanced;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Monitoring
{
    internal sealed class ServiceMetrics : IServiceMetrics, IStartable
    {
        internal const string MetricsPrefix = "rook_service";

        private readonly IBuildInfoLabelCollector _buildInfoCollector;
        private readonly IContainerFacade _containerFacade;
        private readonly Gauge _buildInfoGauge;

        public ServiceMetrics(IBuildInfoLabelCollector collector, IContainerFacade containerFacade)
        {
            _buildInfoCollector = collector;
            _containerFacade = containerFacade;

            _buildInfoGauge = Metrics.CreateGauge(
                $"{MetricsPrefix}_build_info",
                "A metric with a constant '1' value and information labels",
                _buildInfoCollector.GetNames());
        }

        public StartupPriority StartupPriority { get; } = StartupPriority.Lowest;

        public void Start()
        {
            _containerFacade.Map<ICollectorRegistry>(DefaultCollectorRegistry.Instance);

            _buildInfoGauge.Labels(_buildInfoCollector.GetValues()).Set(1);
        }


        private readonly Counter _discardedMessageCounter = Metrics.CreateCounter(
            $"{MetricsPrefix}_discarded_messages",
            "Number of discarded messages",
            "reason");

        public void RecordDiscardedMessage(DiscardedMessageReason reason)
        {
            _discardedMessageCounter.Labels(reason.ToString()).Inc();
        }


        private readonly Counter _publishedMessagesCounter = Metrics.CreateCounter(
            $"{MetricsPrefix}_published_messages",
            "Number of published messages");

        public void RecordPublishedMessage()
        {
            _publishedMessagesCounter.Inc();
        }

        // Prometheus standard unit of time is seconds: https://prometheus.io/docs/practices/naming/#base-units
        private readonly Histogram _messageHandlerDurationHistogram = Metrics.CreateHistogram(
            $"{MetricsPrefix}_message_handler_duration_seconds",
            "Time taken for a message to be processed by a handler",
            labelNames: new[] {"handler"});


        public void RecordProcessedMessage(string handlerName, double elapsedMilliseconds)
        {
            _messageHandlerDurationHistogram.Labels(handlerName).Observe(elapsedMilliseconds / 1000);
        }

        private readonly Counter _rabbitMqOpenChannels = Metrics.CreateCounter(
            $"{MetricsPrefix}_open_main_channels",
            "Number of channels open on main exchange");

        public void RecordNewMainChannel()
        {
            _rabbitMqOpenChannels.Inc();
        }
    }
}
