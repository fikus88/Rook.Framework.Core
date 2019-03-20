namespace Rook.Framework.Core.Monitoring
{
    public interface IBuildInfoLabelCollector
    {
        string[] GetNames();
        string[] GetValues();
    }
}