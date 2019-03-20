using System.Collections.Generic;

namespace Rook.Framework.Core.AccountManagement
{
    internal class RelationshipEqualityComparer : IEqualityComparer<Relationship>
    {
        public bool Equals(Relationship relA, Relationship relB)
        {
            if (relA == null && relB == null)
            {
                return true;
            }

            if (relA == null || relB == null)
            {
                return false;
            }

            return relA.ParentNodeId.Equals(relB.ParentNodeId)
                   && relA.ChildNodeId.Equals(relB.ChildNodeId);
        }

        public int GetHashCode(Relationship rel)
        {
            return string.Concat(rel.ParentNodeId, rel.ChildNodeId).GetHashCode();
        }
    }
}