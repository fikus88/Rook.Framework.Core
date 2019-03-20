using System;

namespace Rook.Framework.Core.OrganisationHierarchyCache
{
    public static class OrganisationHierarchyCacheItemExtensions
    {
        public static bool ContainsOrganisationId(this OrganisationHierarchyCacheItem organisationHierarchyCacheItem, Guid id)
        {
            if (organisationHierarchyCacheItem == null)
                return false;

            if (organisationHierarchyCacheItem.OrganisationId == id)
                return true;

            if (organisationHierarchyCacheItem.ChildOrganisations == null)
                return false;

            foreach (var childTree in organisationHierarchyCacheItem.ChildOrganisations)
            {
                var result = childTree.ContainsOrganisationId(id);

                if (result) //do not just return result as this will end recursion if false.
                    return true;
            }

            return false;
        }
    }
}
