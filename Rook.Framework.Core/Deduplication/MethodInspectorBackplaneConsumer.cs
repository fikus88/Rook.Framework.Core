using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;

namespace Rook.Framework.Core.Deduplication
{
    internal class MethodInspectorBackplaneConsumer : BackplaneConsumer<MessageSubscriber.MethodInspector>
    {
        internal readonly IMessageHashes _receivedHashes;

        public MethodInspectorBackplaneConsumer(IMessageHashes receivedHashes)
        {
            _receivedHashes = receivedHashes;
        }

        public override void Consume(MessageSubscriber.MethodInspector value)
        {
            _receivedHashes.Add(value.Hash);
        }
    }
}