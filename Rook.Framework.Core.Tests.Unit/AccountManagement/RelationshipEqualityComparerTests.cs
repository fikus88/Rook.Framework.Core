using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.AccountManagement;

namespace Rook.Framework.Core.Tests.Unit.AccountManagement
{
    [TestClass]
    public class RelationshipEqualityComparerTests
    {
        private RelationshipEqualityComparer _sut;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _sut = new RelationshipEqualityComparer();
        }

        [TestMethod]
        public void Equals_WithBothRelatinshipsNull_ReturnsTrue()
        {
            var result = _sut.Equals(null, null);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Equals_WithFirstRelatinshipNullButNotSecond_ReturnsFalse()
        {
            var result = _sut.Equals(null, new Relationship());

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Equals_WithSecondRelatinshipNullButNotFirst_ReturnsFalse()
        {
            var result = _sut.Equals(new Relationship(), null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Equals_WhereFirstRelationshipNodeIdsAreSameAsSecondRelationshipNodeIds_ReturnsTrue()
        {
            var parentNodeId = Guid.NewGuid();
            var childNodeId = Guid.NewGuid();

            var relationship1 = new Relationship { ParentNodeId = parentNodeId, ChildNodeId = childNodeId };
            var relationship2 = new Relationship { ParentNodeId = parentNodeId, ChildNodeId = childNodeId };

            var result = _sut.Equals(relationship1, relationship2);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Equals_WhereFirstRelationshipNodeIdsAreSameAsSecondRelationshipNodeIds_ReturnsFalse()
        {
            var relationship1 = new Relationship { ParentNodeId = Guid.NewGuid(), ChildNodeId = Guid.NewGuid() };
            var relationship2 = new Relationship { ParentNodeId = Guid.NewGuid(), ChildNodeId = Guid.NewGuid() };

            var result = _sut.Equals(relationship1, relationship2);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Equals_WhereOnlyParentNodeIdsMatch_ReturnsFalse()
        {
            var nodeId = Guid.NewGuid();

            var relationship1 = new Relationship { ParentNodeId = nodeId, ChildNodeId = Guid.NewGuid() };
            var relationship2 = new Relationship { ParentNodeId = nodeId, ChildNodeId = Guid.NewGuid() };

            var result = _sut.Equals(relationship1, relationship2);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Equals_WhereOnlyChildNodeIdsMatch_ReturnsFalse()
        {
            var nodeId = Guid.NewGuid();

            var relationship1 = new Relationship { ParentNodeId = Guid.NewGuid(), ChildNodeId = nodeId };
            var relationship2 = new Relationship { ParentNodeId = Guid.NewGuid(), ChildNodeId = nodeId };

            var result = _sut.Equals(relationship1, relationship2);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Equals_WhereParentAndChildMatchEachOtherAtRelationshipLevel_ReturnsFalse()
        {
            var nodeIdsForRelationship1 = Guid.NewGuid();
            var nodeIdsForRelationship2 = Guid.NewGuid();

            var relationship1 = new Relationship { ParentNodeId = nodeIdsForRelationship1, ChildNodeId = nodeIdsForRelationship1 };
            var relationship2 = new Relationship { ParentNodeId = nodeIdsForRelationship2, ChildNodeId = nodeIdsForRelationship2 };

            var result = _sut.Equals(relationship1, relationship2);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RelationshipEqualityComparer_WhenUsedInDistinctQuery_ReturnsAppropriateList()
        {
            var parentNodeId = Guid.NewGuid();
            var childNodeId = Guid.NewGuid();

            var relationship1 = new Relationship { ParentNodeId = parentNodeId, ChildNodeId = childNodeId };
            var relationship2 = new Relationship { ParentNodeId = parentNodeId, ChildNodeId = childNodeId };
            var relationship3 = new Relationship { ParentNodeId = Guid.NewGuid(), ChildNodeId = Guid.NewGuid() };

            var relationshipList = new List<Relationship> {relationship1, relationship2, relationship3}.Distinct(_sut).ToList();

            Assert.AreEqual(2, relationshipList.Count());
            Assert.AreEqual(1, relationshipList.Count(rel => rel.ParentNodeId.Equals(parentNodeId) && rel.ChildNodeId.Equals(childNodeId)));
        }
    }
}