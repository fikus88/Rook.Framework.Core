using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.OrganisationHierarchyCache;
using Rook.Framework.Core.Utils;

namespace Rook.Framework.Core.Tests.Unit.OrganisationHierarchyCache
{
    [TestClass]
    public class OrganisationHierarchyCacheItemBackplaneConsumerTests
    {
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

        [TestMethod]
        public void ConsumesType_IsExpectedGuidTypeOfOrganisationTreeItem()
        {
            var organisationTreeItemCacheMock = new Mock<IOrganisationHierarchyCache>();

            var sut = new OrganisationHierarchyCacheItemBackplaneConsumer(_loggerMock.Object, organisationTreeItemCacheMock.Object);

            var consumeType = sut.ConsumesType;

            Assert.AreEqual(typeof(OrganisationHierarchyCacheItem).GUID, consumeType);

            organisationTreeItemCacheMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Consume_CallsAddOrUpdate_WithCorrectObjectValue()
        {
            var organisationTreeItemCacheMock = new Mock<IOrganisationHierarchyCache>();

            var sut = new OrganisationHierarchyCacheItemBackplaneConsumer(_loggerMock.Object, organisationTreeItemCacheMock.Object);

            var orgTreeItem = new OrganisationHierarchyCacheItem
            {
                OrganisationId = Guid.NewGuid(),
                Metadata = new CaseInsensitiveDictionary {{"key1", "value1"}, {"key2", "value2"}},
                ChildOrganisations = new List<OrganisationHierarchyCacheItem>
                {
                    new OrganisationHierarchyCacheItem
                    {
                        OrganisationId = Guid.NewGuid(),
                        Metadata = new CaseInsensitiveDictionary(),
                        ChildOrganisations = null
                    },
                    new OrganisationHierarchyCacheItem
                    {
                        OrganisationId = Guid.NewGuid(),
                        Metadata = new CaseInsensitiveDictionary(),
                        ChildOrganisations = null
                    },
                }

            };

            sut.Consume(orgTreeItem);

            organisationTreeItemCacheMock.Verify(x => x.AddOrUpdate(orgTreeItem.OrganisationId, orgTreeItem));

            organisationTreeItemCacheMock.VerifyNoOtherCalls();
        }
    }
}