using System;
using Rook.Framework.Core.Utils;

namespace Rook.Framework.Core.AccountManagement
{
    internal class Relationship
    {
        public Guid ParentNodeId { get; set; }

        public string ParentNodeType { get; set; }

        public CaseInsensitiveDictionary ParentNodeMetadata { get; set; }

        public Guid ChildNodeId { get; set; }

        public string ChildNodeType { get; set; }

        public CaseInsensitiveDictionary ChildNodeMetadata { get; set; }

        public string Type { get; set; }

        public RelationshipDateRange Active { get; set; }
    }
}