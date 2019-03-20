using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Monitoring
{
    public interface ILoggingMetrics
    {
        void RecordLogMessage(LogLevel level);
    }
}
