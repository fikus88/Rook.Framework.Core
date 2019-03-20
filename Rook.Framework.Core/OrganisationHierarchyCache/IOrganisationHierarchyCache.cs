using System;

namespace Rook.Framework.Core.OrganisationHierarchyCache
{
    public interface IOrganisationHierarchyCache
    {
        OrganisationHierarchyCacheItem GetValue(Guid key);
        void AddOrUpdate(Guid key, OrganisationHierarchyCacheItem item);
    }
}