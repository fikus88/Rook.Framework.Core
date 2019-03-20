using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.OrganisationHierarchyCache;

namespace Rook.Framework.Core.Tests.Unit.OrganisationHierarchyCache
{
    [TestClass]
    public class OrganisationHierarchyCacheItemExtensionsTests
    {
        [TestMethod]
        public void ContainsOrganisationId_WithOrganisationTreeItemEqualsNull_ReturnsFalse()
        {
            var targetId = Guid.NewGuid();

            var sut = (OrganisationHierarchyCacheItem)null;

            //act
            var result = sut.ContainsOrganisationId(targetId);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ContainsOrganisationId_WithIdAtTopLevel_ReturnsTrue()
        {
            var targetId = Guid.NewGuid();

            var sut = new OrganisationHierarchyCacheItem { OrganisationId = targetId };

            //act
            var result = sut.ContainsOrganisationId(targetId);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ContainsOrganisationId_WithMultiLevelTreeThatDoesNotContainId_ReturnsFalse()
        {
            var targetId = Guid.NewGuid();

            var bottomLevelTree = new OrganisationHierarchyCacheItem
            {
                OrganisationId = Guid.NewGuid()
            };
            var middleLevelTree = new OrganisationHierarchyCacheItem
            {
                OrganisationId = Guid.NewGuid(),
                ChildOrganisations = new List<OrganisationHierarchyCacheItem> { bottomLevelTree }
            };
            var sut = new OrganisationHierarchyCacheItem
            {
                OrganisationId = Guid.NewGuid(),
                ChildOrganisations = new List<OrganisationHierarchyCacheItem> { middleLevelTree }
            };

            //act
            var result = sut.ContainsOrganisationId(targetId);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ContainsOrganisationId_WithMultiLevelTreeThatDoesContainId_ReturnsTrue()
        {
            var targetId = Guid.NewGuid();

            var bottomLevelTree = new OrganisationHierarchyCacheItem
            {
                OrganisationId = targetId
            };
            var middleLevelTree = new OrganisationHierarchyCacheItem
            {
                OrganisationId = Guid.NewGuid(),
                ChildOrganisations = new List<OrganisationHierarchyCacheItem> { bottomLevelTree }
            };
            var sut = new OrganisationHierarchyCacheItem
            {
                OrganisationId = Guid.NewGuid(),
                ChildOrganisations = new List<OrganisationHierarchyCacheItem> { middleLevelTree }
            };

            //act
            var result = sut.ContainsOrganisationId(targetId);

            Assert.IsTrue(result);
        }
    }
}
