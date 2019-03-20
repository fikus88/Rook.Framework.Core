using System;
using System.Collections.Generic;
using System.Threading;

namespace Rook.Framework.Core.Deduplication
{
    internal interface IMessageHashes : IDisposable
    {
        void Add(ulong hash);
        bool Check(ulong hash);
    }

    internal class MessageHashes : IMessageHashes
    {
        private IDictionary<ulong, DateTime> ReceivedHashes { get; } = new Dictionary<ulong, DateTime>();
        private readonly Timer janitor;

        public MessageHashes()
        {
            janitor = new Timer(TidyUp, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public void Add(ulong hash)
        {
            lock (ReceivedHashes)
            {
                if (!ReceivedHashes.ContainsKey(hash))
                    ReceivedHashes.Add(hash, DateTime.UtcNow);
            }
        }

        public bool Check(ulong hash)
        {
            return ReceivedHashes.ContainsKey(hash);
        }

        internal void TidyUp(object state)
        {
            lock (ReceivedHashes)
            {
                List<ulong> removals = new List<ulong>();
                foreach (var item in ReceivedHashes)
                {
                    if (DateTime.UtcNow - item.Value > TimeSpan.FromHours(6))
                        removals.Add(item.Key);
                }

                foreach (ulong item in removals)
                    ReceivedHashes.Remove(item);
            }
        }

        public void Dispose()
        {
            janitor?.Dispose();
        }
    }
}