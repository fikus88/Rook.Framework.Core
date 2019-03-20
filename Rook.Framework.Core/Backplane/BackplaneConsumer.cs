using System;

namespace Rook.Framework.Core.Backplane
{
    public abstract class BackplaneConsumer<T> : IBackplaneConsumer
    {
        public Guid ConsumesType { get; } = typeof(T).GUID;

        public void Consume(object v)
        {
            Consume((T)v);
        }

        public abstract void Consume(T value);
    }
}
