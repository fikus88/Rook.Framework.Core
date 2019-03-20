using System;

namespace Rook.Framework.Core.Backplane
{
    [Obsolete("If possible, inherit BackplaneConsumer<T> instead.")]
    public interface IBackplaneConsumer
    {
        Guid ConsumesType { get; }
        void Consume(object v);
    }
}
