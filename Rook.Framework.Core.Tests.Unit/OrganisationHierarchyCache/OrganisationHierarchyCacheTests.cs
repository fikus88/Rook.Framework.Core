using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Rook.Framework.Core.AccountManagement;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.OrganisationHierarchyCache;
using Rook.Framework.Core.Utils;

namespace Rook.Framework.Core.Tests.Unit.OrganisationHierarchyCache
{
    [TestClass]
    public class OrganisationHierarchyCacheTests
    {
        private const string AssetTypeOrganisation = "organisation";
        private const string BusinessToBusinessRelationshipType = "parent of";

        private const string QueryChildRelationshipsMethodName = "QueryChildRelationships";
        private const string GetOrganisationMethodName = "GetOrganisation";

        private Mock<ILogger> _loggerMock;
        private Mock<IRequestStore> _requestStoreMock;
        private Mock<IBackplane> _backplaneMock;
        private Mock<IDateTimeProvider> _dateTimeProviderMock;
        private Mock<IConfigurationManager> _configurationManagerMock;

        private Guid _orgA1, _orgB1, _orgB2, _orgC1, _orgC2, _orgD1, _orgD2, _orgD3, _orgD4, _orgD5, _orgD6;

        private IReadOnlyCollection<Relationship> _relationshipSampleData;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();
            _loggerMock = new Mock<ILogger>();
            _requestStoreMock = new Mock<IRequestStore>();
            _backplaneMock = new Mock<IBackplane>();
            _configurationManagerMock = new Mock<IConfigurationManager>();

            _configurationManagerMock.Setup(x => x.Get("OrganisationCacheTimeout", It.IsAny<int>())).Returns(86400);

            SetupTestData();
        }

        [TestMethod]
        public void GetValue_WithMatchingKeyNotInInCache_WithMultipleMultilevelRelationships_PublishesMesageOntoBusButReturnsItem()
        {
            var key = _orgA1;

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution, It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = JsonConvert.SerializeObject(_relationshipSampleData) });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var result = sut.GetValue(key);

            Assert.IsNotNull(result);
            Assert.IsTrue(ResultDataTreeIsCorrect(result));

            _requestStoreMock.Verify(x =>
                    x.PublishAndWaitForResponse(
                        It.Is<Message<object, Relationship>>(m =>
                            m.Method.Equals(QueryChildRelationshipsMethodName)
                            && TestUtils.GetPropertyValue<Guid>(m.Need, "StartNodeId").Equals(_orgA1)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").From.Equals(mockUtcNow)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").To == null
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "LinkRelationshipTypes").Single().Equals(BusinessToBusinessRelationshipType)
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "NodeTypes").Single().Equals(AssetTypeOrganisation)),
                        ResponseStyle.WholeSolution,
                        It.IsAny<Func<string, bool>>()
                        ), Times.Exactly(1)
            );

            _backplaneMock.Verify(x => x.Send(It.Is<OrganisationHierarchyCacheItem>(oti => oti.OrganisationId != Guid.Empty)));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetValue_WithNoMatchingKeyInCache_NoRelationshipsInRelationshipResponse_PublishesGetOrganisationMessage_CachesResponseCorrectlyAndReturnsResult()
        {
            var key = _orgA1;

            //first message to get relationships
            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution,
                    It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = JsonConvert.SerializeObject(new List<Relationship>()) });

            //second message to get organisation
            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<Guid, OrganisationHierarchyCacheItem>>(), ResponseStyle.FirstOrDefault,
                    null))
                .Returns(new JsonBusResponse
                {
                    Solution = JsonConvert.SerializeObject(new
                    {
                        OrganisationId = _orgA1,
                        Metadata = new CaseInsensitiveDictionary { { "key1", "value1" } }
                    })
                });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var result = sut.GetValue(key);

            Assert.IsNotNull(result);
            Assert.AreEqual(_orgA1, result.OrganisationId);
            
            _requestStoreMock.Verify(x =>
                    x.PublishAndWaitForResponse(
                        It.Is<Message<object, Relationship>>(m =>
                            m.Method.Equals(QueryChildRelationshipsMethodName)
                            && TestUtils.GetPropertyValue<Guid>(m.Need, "StartNodeId").Equals(_orgA1)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").From.Equals(mockUtcNow)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").To == null
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "LinkRelationshipTypes").Single().Equals(BusinessToBusinessRelationshipType)
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "NodeTypes").Single().Equals(AssetTypeOrganisation)),
                        ResponseStyle.WholeSolution,
                        It.IsAny<Func<string, bool>>()
                    ), Times.Exactly(1)
            );

            _requestStoreMock.Verify(x =>
                x.PublishAndWaitForResponse(
                    It.Is<Message<Guid, OrganisationHierarchyCacheItem>>(m =>
                        m.Method.Equals(GetOrganisationMethodName)
                        && m.Need.Equals(_orgA1)),
                    ResponseStyle.FirstOrDefault,
                    null),
                Times.Exactly(1));

            _backplaneMock.Verify(x => x.Send(It.Is<OrganisationHierarchyCacheItem>(oti => oti.OrganisationId.Equals(_orgA1))));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetValue_WithValidKeyAlreadyInCache_MessageIsOnlyPublishedOnceAndCacheIsUsedThereafter()
        {
            var key = _orgA1;

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution, It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = JsonConvert.SerializeObject(_relationshipSampleData) });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var resultFromPublishingMessage = sut.GetValue(key);
            var resultFromCachedItem = sut.GetValue(key);

            Assert.IsNotNull(resultFromPublishingMessage);
            Assert.IsTrue(ResultDataTreeIsCorrect(resultFromPublishingMessage));

            Assert.IsNotNull(resultFromCachedItem);
            Assert.IsTrue(ResultDataTreeIsCorrect(resultFromCachedItem));

            _requestStoreMock.Verify(x =>
                    x.PublishAndWaitForResponse(
                        It.Is<Message<object, Relationship>>(m =>
                            m.Method.Equals(QueryChildRelationshipsMethodName)
                            && TestUtils.GetPropertyValue<Guid>(m.Need, "StartNodeId").Equals(_orgA1)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").From.Equals(mockUtcNow)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").To == null
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "LinkRelationshipTypes").Single().Equals(BusinessToBusinessRelationshipType)
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "NodeTypes").Single().Equals(AssetTypeOrganisation)
                        ),
                        ResponseStyle.WholeSolution,
                        It.IsAny<Func<string, bool>>()
                        ), Times.Once //Even though we called GetValue twice, we only published a message once proving the entry was cached.
            );

            _backplaneMock.Verify(x => x.Send(It.Is<OrganisationHierarchyCacheItem>(oti => oti.OrganisationId != Guid.Empty)));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetValue_WithValidKeyAlreadyInCache_ButCacheTimeSetToZero_MessageIsPublishedTwice()
        {
            _configurationManagerMock.Setup(x => x.Get("OrganisationCacheTimeout", It.IsAny<int>())).Returns(0);//cache time of zero.

            var key = _orgA1;

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution, It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = JsonConvert.SerializeObject(_relationshipSampleData) });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var resultFromPublishingMessage = sut.GetValue(key);
            var resultFromCachedItem = sut.GetValue(key);

            Assert.IsNotNull(resultFromPublishingMessage);
            Assert.IsTrue(ResultDataTreeIsCorrect(resultFromPublishingMessage));

            Assert.IsNotNull(resultFromCachedItem);
            Assert.IsTrue(ResultDataTreeIsCorrect(resultFromCachedItem));

            _requestStoreMock.Verify(x =>
                    x.PublishAndWaitForResponse(
                        It.Is<Message<object, Relationship>>(m =>
                            m.Method.Equals(QueryChildRelationshipsMethodName)
                            && TestUtils.GetPropertyValue<Guid>(m.Need, "StartNodeId").Equals(_orgA1)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").From.Equals(mockUtcNow)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").To == null
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "LinkRelationshipTypes").Single().Equals(BusinessToBusinessRelationshipType)
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "NodeTypes").Single().Equals(AssetTypeOrganisation)
                        ),
                        ResponseStyle.WholeSolution,
                        It.IsAny<Func<string, bool>>()
                        ), Times.Exactly(2) //We called get value twice, which would have cached it. but cache time set to zero which means the call would have been made twice as the cache would have been immediately invalidated.
            );

            _backplaneMock.Verify(x => x.Send(It.Is<OrganisationHierarchyCacheItem>(oti => oti.OrganisationId != Guid.Empty)));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetValue_RelationshipsReturnedFromQueryChildRelationshipsContainRequestedOrganisationIdAsAChild_LogsAndThrowsException()
        {
            const string error = "The expected root node for the tree should be a parent and should not appear as a child in any relationship.";

            var key = _orgA1;

            var invalidRelationships = new List<Relationship>
            {
                new Relationship {ChildNodeId = Guid.NewGuid(), ParentNodeId = Guid.NewGuid()},
                new Relationship {ChildNodeId = _orgA1, ParentNodeId = Guid.NewGuid()}
            };

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution, It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = JsonConvert.SerializeObject(invalidRelationships) });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            try
            {
                sut.GetValue(key);
                Assert.Fail("Should have thrown an exception.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, error);
            }
            
            _requestStoreMock.Verify(x =>
                    x.PublishAndWaitForResponse(
                        It.Is<Message<object, Relationship>>(m =>
                            m.Method.Equals(QueryChildRelationshipsMethodName)
                            && TestUtils.GetPropertyValue<Guid>(m.Need, "StartNodeId").Equals(_orgA1)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").From.Equals(mockUtcNow)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").To == null
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "LinkRelationshipTypes").Single().Equals(BusinessToBusinessRelationshipType)
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "NodeTypes").Single().Equals(AssetTypeOrganisation)
                        ),
                        ResponseStyle.WholeSolution,
                        It.IsAny<Func<string, bool>>()
                        ), Times.Once
            );

            _loggerMock.Verify(x => x.Error("OrganisationHierarchyCache.ConvertRelationshipsToOrganisationTreeItem",
                It.Is<LogItem[]>(li => li[0].Key.Equals("Error")
                                       && li[0].Value.Invoke().Equals(error)
                                       && li[1].Key.Equals("rootNode")
                                       && li[1].Value.Invoke().Equals(_orgA1.ToString())
                                       )));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetValue_RelationshipsReturnedFromQueryChildRelationshipsDoNotContainRequestedOrganisationIdAsAParent_LogsAndThrowsException()
        {
            const string error = "The expected root node for the tree should be a parent and should not appear as a child in any relationship.";

            var key = _orgA1;

            var invalidRelationships = new List<Relationship>
            {
                new Relationship {ChildNodeId = Guid.NewGuid(), ParentNodeId = Guid.NewGuid()},
                new Relationship {ChildNodeId = Guid.NewGuid(), ParentNodeId = Guid.NewGuid()}
            };

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution, It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = JsonConvert.SerializeObject(invalidRelationships) });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            try
            {
                sut.GetValue(key);
                Assert.Fail("Should have thrown an exception.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, error);
            }
            
            _requestStoreMock.Verify(x =>
                    x.PublishAndWaitForResponse(
                        It.Is<Message<object, Relationship>>(m =>
                            m.Method.Equals(QueryChildRelationshipsMethodName)
                            && TestUtils.GetPropertyValue<Guid>(m.Need, "StartNodeId").Equals(_orgA1)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").From.Equals(mockUtcNow)
                            && TestUtils.GetPropertyValue<RelationshipDateRange>(m.Need, "Active").To == null
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "LinkRelationshipTypes").Single().Equals(BusinessToBusinessRelationshipType)
                            && TestUtils.GetPropertyValue<string[]>(m.Need, "NodeTypes").Single().Equals(AssetTypeOrganisation)
                        ),
                        ResponseStyle.WholeSolution,
                        It.IsAny<Func<string, bool>>()
                        ), Times.Once
            );

            _loggerMock.Verify(x => x.Error("OrganisationHierarchyCache.ConvertRelationshipsToOrganisationTreeItem",
                It.Is<LogItem[]>(li => li[0].Key.Equals("Error")
                                       && li[0].Value.Invoke().Equals(error)
                                       && li[1].Key.Equals("rootNode")
                                       && li[1].Value.Invoke().Equals(_orgA1.ToString())
                                       )));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void AddOrUpdate_WithInitialAddEntry_ThenUpdateEntry_AddsAndUpdatesEntryCorrectlyAndNoMessagesArePublished()
        {
            var key = new Guid("bc44ffe6-0533-47c8-986b-e40347a4d072");
            var initialValue = new OrganisationHierarchyCacheItem { OrganisationId = key, ChildOrganisations = null, Metadata = null };
            var updatedValue = new OrganisationHierarchyCacheItem { OrganisationId = key, ChildOrganisations = new List<OrganisationHierarchyCacheItem>(), Metadata = new CaseInsensitiveDictionary { { "key1", "value1" } } };

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            sut.AddOrUpdate(key, initialValue);
            Assert.AreEqual(initialValue, sut.GetValue(key));

            sut.AddOrUpdate(key, updatedValue);
            Assert.AreEqual(updatedValue, sut.GetValue(key));

            _requestStoreMock.VerifyNoOtherCalls();
            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void BusResponseSolutionContainsErrors_ErrorsAreLogged_ExceptionIsRaised()
        {
            var key = _orgA1;

            const string mockError = "mock error";
            const string eventDescription = "Bus response contained errors.";

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution,
                    It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse { Solution = null, Errors = mockError });

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            try
            {
                sut.GetValue(key);
                Assert.Fail("Should have thrown an exception.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Equals("mock error", StringComparison.OrdinalIgnoreCase));
            }
            
            _loggerMock.Verify(x => x.Error("OrganisationHierarchyCache.GetOrganisationTreeItemForOrganisation",
                It.Is<LogItem[]>(li =>
                    li[0].Key.Equals("Event")
                    && li[0].Value.Invoke().Equals(eventDescription)
                    && li[1].Key.Equals("organisationId")
                    && li[1].Value.Invoke().Equals(_orgA1.ToString())
                    && li[2].Key.Equals("Errors")
                    && li[2].Value.Invoke().Equals(mockError)
                )));

            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void BusResponseSolutionContainsNullSolutionAndNoErrors_ExceptionIsRaised()
        {
            var key = _orgA1;

            _requestStoreMock.Setup(x => x.PublishAndWaitForResponse(
                    It.IsAny<Message<object, Relationship>>(), ResponseStyle.WholeSolution,
                    It.IsAny<Func<string, bool>>()))
                .Returns(new JsonBusResponse());

            var mockUtcNow = new DateTime(2017, 2, 2, 4, 3, 1);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(mockUtcNow);

            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            try
            {
                sut.GetValue(key);
                Assert.Fail("Should have thrown an exception.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Equals("Bus response contained no solution. Most likely no response was received in time.", StringComparison.OrdinalIgnoreCase));
            }
            
            _loggerMock.Verify(x => x.Error("OrganisationHierarchyCache.GetOrganisationTreeItemForOrganisation",
                It.Is<LogItem[]>(li =>
                    li[0].Key.Equals("Event")
                    && li[0].Value.Invoke().Equals("Bus response contained no solution. Most likely no response was received in time.")
                    && li[1].Key.Equals("organisationId")
                    && li[1].Value.Invoke().Equals(_orgA1.ToString())
                )));

            _backplaneMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SolutionMatchFunctionForAllMetadataNotNull_WithAllMetadataNotNull_ReturnsTrue()
        {
            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var relationships = new List<Relationship> {
            new Relationship
            {
                ParentNodeMetadata = new CaseInsensitiveDictionary(),
                ChildNodeMetadata = new CaseInsensitiveDictionary()
            }};

            var solution = JsonConvert.SerializeObject(relationships);

            var result = sut.SolutionMatchFunctionForAllMetadataNotNull(solution);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SolutionMatchFunctionForAllMetadataNotNull_WithParentMetadataNull_ReturnsFalse()
        {
            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var relationships = new List<Relationship> {
                new Relationship
                {
                    ParentNodeMetadata = null,
                    ChildNodeMetadata = new CaseInsensitiveDictionary()
                }};

            var solution = JsonConvert.SerializeObject(relationships);

            var result = sut.SolutionMatchFunctionForAllMetadataNotNull(solution);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SolutionMatchFunctionForAllMetadataNotNull_WithChildMetadataNull_ReturnsFalse()
        {
            var sut = new Core.OrganisationHierarchyCache.OrganisationHierarchyCache(_loggerMock.Object, _requestStoreMock.Object, _dateTimeProviderMock.Object, _backplaneMock.Object, _configurationManagerMock.Object);

            var relationships = new List<Relationship> {
                new Relationship
                {
                    ParentNodeMetadata = new CaseInsensitiveDictionary(),
                    ChildNodeMetadata = null
                }};

            var solution = JsonConvert.SerializeObject(relationships);

            var result = sut.SolutionMatchFunctionForAllMetadataNotNull(solution);

            Assert.IsFalse(result);
        }

        private void SetupTestData()
        {
            /*
                                                     [orgA1]
                                                        |
                            ------------------------------------------------------------
                            |                                           |              |
                         [orgB1]                                     [orgB2]           |
                           |---------------------\           /----------|              |
                           |                      \         /           |              |
                         [orgC2]                   \       /         [orgC2]           |
                           |                        \     /             |              |
              ----------------------------           \   /              |              |
              |            |             |            \ /               |              |
            [orgD1]     [orgD2]       [orgD3]       [orgD4]          [orgD5]        [orgD6]

            */

            _orgA1 = new Guid("A1ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgB1 = new Guid("B1ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgB2 = new Guid("B2ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgC1 = new Guid("C1ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgC2 = new Guid("C2ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgD1 = new Guid("D1ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgD2 = new Guid("D2ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgD3 = new Guid("D3ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgD4 = new Guid("D4ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgD5 = new Guid("D5ddd18a-f19c-42e9-9d6a-e05ea02802cb");
            _orgD6 = new Guid("D6ddd18a-f19c-42e9-9d6a-e05ea02802cb");

            _relationshipSampleData = new List<Relationship>
            {
                new Relationship {ParentNodeId = _orgA1, ChildNodeId = _orgB1, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgA1.ToString(), _orgA1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgB1.ToString(), _orgB1.ToString()}}},
                new Relationship {ParentNodeId = _orgA1, ChildNodeId = _orgB2, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgA1.ToString(), _orgA1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgB2.ToString(), _orgB2.ToString()}}},
                new Relationship {ParentNodeId = _orgA1, ChildNodeId = _orgD6, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgA1.ToString(), _orgA1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD6.ToString(), _orgD6.ToString()}}},
                new Relationship {ParentNodeId = _orgB1, ChildNodeId = _orgC1, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgB1.ToString(), _orgB1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgC1.ToString(), _orgC1.ToString()}}},
                new Relationship {ParentNodeId = _orgB1, ChildNodeId = _orgD4, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgB1.ToString(), _orgB1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD4.ToString(), _orgD4.ToString()}}},
                new Relationship {ParentNodeId = _orgB2, ChildNodeId = _orgD4, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgB2.ToString(), _orgB2.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD4.ToString(), _orgD4.ToString()}}},
                new Relationship {ParentNodeId = _orgB2, ChildNodeId = _orgC2, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgB2.ToString(), _orgB2.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgC2.ToString(), _orgC2.ToString()}}},
                new Relationship {ParentNodeId = _orgC1, ChildNodeId = _orgD1, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgC1.ToString(), _orgC1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD1.ToString(), _orgD1.ToString()}}},
                new Relationship {ParentNodeId = _orgC1, ChildNodeId = _orgD2, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgC1.ToString(), _orgC1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD2.ToString(), _orgD2.ToString()}}},
                new Relationship {ParentNodeId = _orgC1, ChildNodeId = _orgD3, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgC1.ToString(), _orgC1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD3.ToString(), _orgD3.ToString()}}},
                new Relationship {ParentNodeId = _orgC2, ChildNodeId = _orgD5, ParentNodeMetadata = new CaseInsensitiveDictionary{{_orgC2.ToString(), _orgC2.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary{{_orgD5.ToString(), _orgD5.ToString()}}},
                
                //add some duplciate rows to ensure duplicates are not transposed into recursive object.
                new Relationship {ParentNodeId = _orgB2, ChildNodeId = _orgC2, ParentNodeMetadata = new CaseInsensitiveDictionary {{_orgB2.ToString(), _orgB2.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary {{_orgC2.ToString(), _orgC2.ToString()}}},
                new Relationship {ParentNodeId = _orgC1, ChildNodeId = _orgD3, ParentNodeMetadata = new CaseInsensitiveDictionary {{_orgC1.ToString(), _orgC1.ToString()}}, ChildNodeMetadata = new CaseInsensitiveDictionary {{_orgD3.ToString(), _orgD3.ToString()}}}
            };
        }

        private bool ResultDataTreeIsCorrect(OrganisationHierarchyCacheItem result)
        {
            //work down the tree and check the child counts and ids of children.
            Assert.AreEqual(result.OrganisationId, _orgA1, "Top level Id expected to be _orgA1");
            Assert.AreEqual(_orgA1.ToString(), result.Metadata[_orgA1.ToString()]);

            Assert.AreEqual(3, result.ChildOrganisations.Count, "Expected top level to have 3 children");
            Assert.IsNotNull(result.ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgB1), "Expected B1 as child id");
            Assert.IsNotNull(result.ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgB2), "Expected B2 as child id");
            Assert.IsNotNull(result.ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgD6), "Expected D6 as child id");

            Assert.AreEqual(2, result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.Count, "Expected B1 to have 2 children");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgC1), "Expected C1 as child id");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgD4), "Expected D4 as child id");

            Assert.AreEqual(2, result.ChildOrganisations.Single(x => x.OrganisationId == _orgB2).ChildOrganisations.Count, "Expected B2 to have 2 children");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB2).ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgC2), "Expected C2 as child id");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB2).ChildOrganisations.SingleOrDefault(x => x.OrganisationId == _orgD4), "Expected D4 as child id");

            Assert.AreEqual(3, result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.Single(x => x.OrganisationId == _orgC1).ChildOrganisations.Count, "Expected C1 to have 3 children");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.Single(x => x.OrganisationId == _orgC1).ChildOrganisations.SingleOrDefault(x => x.OrganisationId.Equals(_orgD1)), "Expected D1 as child id");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.Single(x => x.OrganisationId == _orgC1).ChildOrganisations.SingleOrDefault(x => x.OrganisationId.Equals(_orgD2)), "Expected D2 as child id");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB1).ChildOrganisations.Single(x => x.OrganisationId == _orgC1).ChildOrganisations.SingleOrDefault(x => x.OrganisationId.Equals(_orgD3)), "Expected D3 as child id");

            Assert.AreEqual(1, result.ChildOrganisations.Single(x => x.OrganisationId == _orgB2).ChildOrganisations.Single(x => x.OrganisationId == _orgC2).ChildOrganisations.Count, "Expected C2 to have 1 child");
            Assert.IsNotNull(result.ChildOrganisations.Single(x => x.OrganisationId == _orgB2).ChildOrganisations.Single(x => x.OrganisationId == _orgC2).ChildOrganisations.SingleOrDefault(x => x.OrganisationId.Equals(_orgD5)), "Expected D5 as child id");

            return true;
        }
    }
}
