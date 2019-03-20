using System;
using System.Collections.Generic;
using Rook.Framework.Core.Utils;

namespace Rook.Framework.Core.OrganisationHierarchyCache
{
    public class OrganisationHierarchyCacheItem
    {
        public Guid OrganisationId { get; set; }
        public CaseInsensitiveDictionary Metadata { get; set; }
        public List<OrganisationHierarchyCacheItem> ChildOrganisations { get; set; }
    }
}