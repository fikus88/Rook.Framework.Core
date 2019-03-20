using System;

namespace Rook.Framework.Core.AccountManagement
{
    internal class RelationshipDateRange
    {
        public RelationshipDateRange(DateTime from, DateTime? to)
        {
            From = from;
            To = to;
        }
        public DateTime From { get; }
        public DateTime? To { get; }
    }
}