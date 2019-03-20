using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.OrganisationHierarchyCache
{
    internal class OrganisationHierarchyCacheItemBackplaneConsumer : BackplaneConsumer<OrganisationHierarchyCacheItem>
    {
        private readonly ILogger _logger;
        private readonly IOrganisationHierarchyCache _cache;

        public OrganisationHierarchyCacheItemBackplaneConsumer(ILogger logger, IOrganisationHierarchyCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public override void Consume(OrganisationHierarchyCacheItem organisationCacheItem)
        {
            _logger.Trace($"{nameof(OrganisationHierarchyCacheItemBackplaneConsumer)}.{nameof(Consume)}",
                new LogItem($"{nameof(OrganisationHierarchyCacheItem)}.{nameof(OrganisationHierarchyCacheItem.OrganisationId)}",organisationCacheItem.OrganisationId.ToString())
            );

            _cache.AddOrUpdate(organisationCacheItem.OrganisationId, organisationCacheItem);
        }
    }
}