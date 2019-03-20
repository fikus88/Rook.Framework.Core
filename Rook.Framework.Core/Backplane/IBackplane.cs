namespace Rook.Framework.Core.Backplane
{
    public interface IBackplane
    {
        void Send<T>(T data);
    }
}
