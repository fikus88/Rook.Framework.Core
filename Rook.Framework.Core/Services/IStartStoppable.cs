namespace Rook.Framework.Core.Services
{
    public interface IStartStoppable : IStartable
    {
        void Stop();
    }
}
