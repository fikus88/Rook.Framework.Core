using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;
using static Rook.Framework.Core.Application.Subscribe.MessageSubscriber;

namespace Rook.Framework.Core.Deduplication
{
    internal class MethodInspectorManager : IMethodInspectorManager
    {
        private readonly IBackplane backplane;
        internal readonly IMessageHashes _receivedHashes;

        public MethodInspectorManager(IBackplane backplane, IMessageHashes receivedHashes)
        {
            this.backplane = backplane;
            _receivedHashes = receivedHashes;
        }

        public bool DuplicateCheck(MessageSubscriber.MethodInspector inspector)
        {
            return _receivedHashes.Check(inspector.Hash);
        }
        
        public void Register(MessageSubscriber.MethodInspector inspector)
        {
            backplane.Send(inspector);
        }
    }
}
