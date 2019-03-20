using Rook.Framework.Core.Application.Subscribe;
using static Rook.Framework.Core.Application.Subscribe.MessageSubscriber;

namespace Rook.Framework.Core.Deduplication
{
    public interface IMethodInspectorManager
    {
        bool DuplicateCheck(MessageSubscriber.MethodInspector m);
        void Register(MessageSubscriber.MethodInspector inspector);
    }
}