using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rook.Framework.Core.AccountManagement;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.OrganisationHierarchyCache
{
    internal class OrganisationHierarchyCache : IOrganisationHierarchyCache
    {
        private const string QueryChildRelationshipsMethodName = "QueryChildRelationships";
        private const string GetOrganisationMethodName = "GetOrganisation";

        private static readonly string OperationGetOrgTree = $"{nameof(OrganisationHierarchyCache)}.{nameof(GetOrganisationTreeItemForOrganisation)}";
        private static readonly string OperationConvertRelationships = $"{nameof(OrganisationHierarchyCache)}.{nameof(ConvertRelationshipsToOrganisationTreeItem)}";

        private const string AssetTypeOrganisation = "organisation";
        private const string BusinessToBusinessRelationshipType = "parent of";

        private readonly ILogger _logger;
        private readonly IRequestStore _requestStore;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBackplane _backplane;

        private readonly CacheList<Guid, OrganisationHierarchyCacheItem> _cacheList;

        public OrganisationHierarchyCache(
            ILogger logger,
            IRequestStore requestStore,
            IDateTimeProvider dateTimeProvider,
            IBackplane backplane,
            IConfigurationManager configurationManager
            )
        {
            _logger = logger;
            _requestStore = requestStore;
            _dateTimeProvider = dateTimeProvider;
            _backplane = backplane;

            var timeoutInSeconds = configurationManager.Get("OrganisationCacheTimeout", 0);//zero if not configured, this avoids confusion with things being auto cached.

            _logger.Debug($"{nameof(OrganisationHierarchyCache)}", new LogItem("Event", $"Organisation cache timeout set to {timeoutInSeconds}"));

            _cacheList = new CacheList<Guid, OrganisationHierarchyCacheItem>(GetOrganisationTreeItemForOrganisation, TimeSpan.FromSeconds(timeoutInSeconds), false);
        }

        public OrganisationHierarchyCacheItem GetValue(Guid key)
        {
            return _cacheList[key];
        }

        public void AddOrUpdate(Guid key, OrganisationHierarchyCacheItem item)
        {
            _cacheList[key] = item;
        }

        private OrganisationHierarchyCacheItem GetOrganisationTreeItemForOrganisation(Guid organisationId)
        {
            _logger.Trace(OperationGetOrgTree, new LogItem(nameof(organisationId), organisationId.ToString()));

            var queryChildRelationshipsMessage = new Message<object, Relationship>
            {
                Method = QueryChildRelationshipsMethodName,
                Need = new
                {
                    StartNodeId = organisationId,
                    Active = new RelationshipDateRange(_dateTimeProvider.UtcNow, null),
                    LinkRelationshipTypes = new[] { BusinessToBusinessRelationshipType },
                    NodeTypes = new[] { AssetTypeOrganisation }
                }
            };

            var queryChildRelationshipsResponse = _requestStore.PublishAndWaitForResponse(queryChildRelationshipsMessage, ResponseStyle.WholeSolution, SolutionMatchFunctionForAllMetadataNotNull);

            CheckResponse(queryChildRelationshipsResponse, organisationId);

            var relationshipsForOrganisation = JsonConvert.DeserializeObject<List<Relationship>>(queryChildRelationshipsResponse.Solution);

            OrganisationHierarchyCacheItem organisationTreeItem;

            if (relationshipsForOrganisation.Any())
            {
                organisationTreeItem = ConvertRelationshipsToOrganisationTreeItem(relationshipsForOrganisation, organisationId);
            }
            else
            {
                //no relationships exist, so get the requested organisation and cast it to an organisation tree item so it can be cached.
                var getOrganisationMessage = new Message<Guid, OrganisationHierarchyCacheItem>
                {
                    Method = GetOrganisationMethodName,
                    Need = organisationId
                };

                var getOrganisationResponse = _requestStore.PublishAndWaitForResponse(getOrganisationMessage, ResponseStyle.FirstOrDefault);

                CheckResponse(getOrganisationResponse, organisationId);

                organisationTreeItem = JsonConvert.DeserializeObject<OrganisationHierarchyCacheItem>(getOrganisationResponse.Solution);
                organisationTreeItem.OrganisationId = organisationId;
                organisationTreeItem.ChildOrganisations = null;
            }

            if (organisationTreeItem != null)
            {
                //publish result to the backplane to update other caches
                _backplane.Send(organisationTreeItem);
            }
            else
            {
                _logger.Warn(OperationGetOrgTree,
                    new LogItem("Event", "Org tree was null, skipping publish"));
            }
            

            return organisationTreeItem;
        }

        private void CheckResponse(JsonBusResponse busResponse, Guid organisationId)
        {
            if (!string.IsNullOrWhiteSpace(busResponse.Errors))
            {
                _logger.Error(OperationGetOrgTree,
                    new LogItem("Event", "Bus response contained errors."),
                    new LogItem(nameof(organisationId), organisationId.ToString()),
                    new LogItem("Errors", busResponse.Errors));

                throw new InvalidOperationException(busResponse.Errors);
            }

            if (string.IsNullOrWhiteSpace(busResponse.Solution))
            {
                const string eventError = "Bus response contained no solution. Most likely no response was received in time.";
                _logger.Error(OperationGetOrgTree,
                    new LogItem("Event", eventError),
                    new LogItem(nameof(organisationId), organisationId.ToString()));

                throw new InvalidOperationException(eventError);
            }
        }

        private OrganisationHierarchyCacheItem ConvertRelationshipsToOrganisationTreeItem(IReadOnlyCollection<Relationship> relationships, Guid rootNode)
        {
            //make sure root node is in list and top level
            if (relationships.All(rel => rootNode != rel.ParentNodeId)
                || relationships.Any(rel => rel.ChildNodeId == rootNode))
            {
                const string error = "The expected root node for the tree should be a parent and should not appear as a child in any relationship.";
                _logger.Error(OperationConvertRelationships,
                    new LogItem("Error", error),
                    new LogItem("rootNode", rootNode.ToString()));
                throw new InvalidOperationException(error);
            }

            var organisationTreeItem = new OrganisationHierarchyCacheItem
            {
                OrganisationId = rootNode
            };
            organisationTreeItem.Metadata = relationships.First(x => x.ParentNodeId == organisationTreeItem.OrganisationId)
                .ParentNodeMetadata;

            RecursivelyPopulateChildrenForOrganisationTreeItems(relationships, organisationTreeItem);

            return organisationTreeItem;
        }

        private static List<OrganisationHierarchyCacheItem> RecursivelyPopulateChildrenForOrganisationTreeItems(IReadOnlyCollection<Relationship> relationships, OrganisationHierarchyCacheItem organisationTreeItem)
        {
            //when we enter, we know the current parentNodeId and metadata being passed in is populated.
            //so we need to now populate the children.

            var directChildRelationshipsForParent = relationships.Where(x => x.ParentNodeId == organisationTreeItem.OrganisationId)
                .Distinct(new RelationshipEqualityComparer()).ToList();

            if (directChildRelationshipsForParent.Any())//continue recursion downward
            {
                organisationTreeItem.ChildOrganisations = new List<OrganisationHierarchyCacheItem>();

                foreach (var relationship in directChildRelationshipsForParent)
                {
                    var childOrgToAdd = new OrganisationHierarchyCacheItem
                    {
                        OrganisationId = relationship.ChildNodeId,
                        Metadata = relationship.ChildNodeMetadata
                    };

                    organisationTreeItem.ChildOrganisations.AddRange(RecursivelyPopulateChildrenForOrganisationTreeItems(relationships, childOrgToAdd));
                }
            }

            return new List<OrganisationHierarchyCacheItem> { organisationTreeItem };
        }

        internal bool SolutionMatchFunctionForAllMetadataNotNull(string solution)
        {
            _logger.Trace($"{nameof(OrganisationHierarchyCache)}.{nameof(SolutionMatchFunctionForAllMetadataNotNull)}", new LogItem("solution", solution));

            var relationships = JsonConvert.DeserializeObject<List<Relationship>>(solution);

            //metadata can be empty but cannot be null, otherwise how dow we know its been decorated.
            return relationships.All(x => x.ParentNodeMetadata != null && x.ChildNodeMetadata != null);
        }
    }
}
