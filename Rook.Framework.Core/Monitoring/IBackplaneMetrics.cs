namespace Rook.Framework.Core.Monitoring
{
    public interface IBackplaneMetrics
    {
        void RecordProcessedMessage(string handlerName, double elapsedMilliseconds);
        void RecordNewBackplaneChannel();
    }
}
