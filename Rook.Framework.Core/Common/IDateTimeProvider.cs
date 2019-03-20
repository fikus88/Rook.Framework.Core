using System;

namespace Rook.Framework.Core.Common
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }

        DateTimeOffset UtcDateTimeOffsetNow { get; }
    }
}