using System;

namespace Rook.Framework.Core.Common
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcDateTimeOffsetNow => DateTimeOffset.UtcNow;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}