using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Deduplication;
using static Rook.Framework.Core.Application.Subscribe.MessageSubscriber;

namespace Rook.Framework.Core.Tests.Unit.Deduplication
{
    [TestClass]
    public class MethodInspectorBackplaneConsumerTests
    {
        private MethodInspectorBackplaneConsumer _sut;
        private MessageHashes _messageHashes;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _messageHashes = new MessageHashes();
            _sut = new MethodInspectorBackplaneConsumer(_messageHashes);
        }

        [TestMethod]
        public void Consume_called_when_TidyUp_in_progress()
        {
            for (ulong i = 0; i < 100000; i++)
            {
                _sut.Consume(new MessageSubscriber.MethodInspector
                {
                    Hash = i
                });
            }

            // Fake TidyUp being triggered while we're still consuming messages
            var tidyUp = new Task(() => _messageHashes.TidyUp(null));
            var consume = new Task(() =>
            {
                // Now throw a load more in:
                for (ulong i = 100000; i < 200000; i++)
                {
                    _sut.Consume(new MessageSubscriber.MethodInspector
                    {
                        Hash = i
                    });
                }
            });

            consume.Start();
            tidyUp.Start();

            Task.WaitAll(tidyUp, consume);
        }
    }
}
