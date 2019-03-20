using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Tests.Unit.HttpServer
{
    [TestClass]
    public class HttpRequestTests
    {
        private const string ClaimTypeForOrganisationIds = "organisationids";
        private const string ClaimTypeForOrganisationIdsAdministers = "organisationidsadministers";

        private string TestRequest = "GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 123\r\n\r\n012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012";
        private string TestRequestHugeContent = "GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 2000\r\n\r\n01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
        private string TestRequestLowerCaseHeaders = "GET /description HTTP/1.1\r\nhost: localhost\r\ncontent-length: 123\r\n\r\n012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012";
        
        [TestMethod]
        public void TestProcessingGetRequestWithProperCaseHeaders()
        {
            MockRequestBroker broker = new MockRequestBroker();

            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            const int port = 8880;
            config.Setup(conf => conf.Get("Port", It.IsAny<int>())).Returns(port);
            Mock<ILogger> logger = new Mock<ILogger>();
            Mock<IContainerFacade> container = new Mock<IContainerFacade>();
            NanoHttp nanoHttp = new NanoHttp(new[] { broker }, config.Object, logger.Object, container.Object);
            try
            {
                nanoHttp.Start();
                Socket client = new Socket(SocketType.Stream, ProtocolType.IP)
                {
                    ReceiveBufferSize = 8192,
                    LingerState = new LingerOption(false, 0),
                    NoDelay = false
                };
                client.Connect("127.0.0.1", 8880);

                client.Send(Encoding.ASCII.GetBytes(TestRequest));
                broker.WaitHandle.WaitOne();

                client.Shutdown(SocketShutdown.Both);
                client.Dispose();
            }
            finally
            {
                nanoHttp.Stop();
            }

            Assert.AreEqual(HttpVerb.Get, broker.Verb);
            Assert.AreEqual(123, broker.Body.Length);
            Assert.AreEqual(48, broker.Body[0]);
            Assert.AreEqual(50, broker.Body[122]);
        }

        [TestMethod]
        public void TestProcessingGetRequestWithLowerCaseHeaders()
        {
            MockRequestBroker broker = new MockRequestBroker();

            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IContainerFacade> container = new Mock<IContainerFacade>();
            const int port = 8881;
            config.Setup(conf => conf.Get("Port", It.IsAny<int>())).Returns(port);
            Mock<ILogger> logger = new Mock<ILogger>();
            NanoHttp nanoHttp = new NanoHttp(new[] { broker }, config.Object, logger.Object, container.Object);
            try
            {
                nanoHttp.Start();
                Socket client = new Socket(SocketType.Stream, ProtocolType.IP)
                {
                    ReceiveBufferSize = 8192,
                    LingerState = new LingerOption(false, 0),
                    NoDelay = false
                };
                client.Connect("127.0.0.1", port);

                client.Send(Encoding.ASCII.GetBytes(TestRequestLowerCaseHeaders));
                broker.WaitHandle.WaitOne();

                client.Shutdown(SocketShutdown.Both);
                client.Dispose();
            }
            finally
            {
                nanoHttp.Stop();
            }
            Assert.AreEqual(HttpVerb.Get, broker.Verb);
            Assert.AreEqual(123, broker.Body.Length);
            Assert.AreEqual(48, broker.Body[0]);
            Assert.AreEqual(50, broker.Body[122]);
        }

        [TestMethod]
        public void TestProcessingGetRequestWithHugeContent()
        {
            MockRequestBroker broker = new MockRequestBroker();

            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IContainerFacade> container = new Mock<IContainerFacade>();
            const int port = 8882;
            config.Setup(conf => conf.Get("Port", It.IsAny<int>())).Returns(port);
            Mock<ILogger> logger = new Mock<ILogger>();
            NanoHttp nanoHttp = new NanoHttp(new[] { broker }, config.Object, logger.Object, container.Object);
            try
            {
                nanoHttp.Start();
                Socket client = new Socket(SocketType.Stream, ProtocolType.IP)
                {
                    ReceiveBufferSize = 1024,
                    LingerState = new LingerOption(false, 0),
                    NoDelay = true
                };
                client.Connect("127.0.0.1", port);

                byte[] bytes = Encoding.ASCII.GetBytes(TestRequestHugeContent);

                client.Send(bytes);

                broker.WaitHandle.WaitOne();

                client.Shutdown(SocketShutdown.Both);
                client.Dispose();
            }
            finally
            {
                nanoHttp.Stop();
            }
            Assert.AreEqual(HttpVerb.Get, broker.Verb);
            Assert.AreEqual(2000, broker.Body.Length);
            Assert.AreEqual(48, broker.Body[0]);
            Assert.AreEqual(57, broker.Body[1999]);
        }

        [TestMethod]
        public void TestProcessingGetRequestWithConsignmentMessageWithSignature()
        {
            string content = File.ReadAllText("BigJsonExample.json");
            string TestConsignmentWithSignatureRequest =
                $"GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: {content.Length}\r\n\r\n{content}";

            MockRequestBroker broker = new MockRequestBroker();

            Mock<IConfigurationManager> config = new Mock<IConfigurationManager>();
            Mock<IContainerFacade> container = new Mock<IContainerFacade>();
            const int port = 8883;
            config.Setup(conf => conf.Get("Port", It.IsAny<int>())).Returns(port);
            Mock<ILogger> logger = new Mock<ILogger>();
            NanoHttp nanoHttp = new NanoHttp(new[] { broker }, config.Object, logger.Object, container.Object);
            try
            {
                nanoHttp.Start();
                Socket client = new Socket(SocketType.Stream, ProtocolType.IP)
                {
                    ReceiveBufferSize = 1024,
                    LingerState = new LingerOption(false, 0),
                    NoDelay = true
                };
                client.Connect("127.0.0.1", port);

                byte[] bytes = Encoding.ASCII.GetBytes(TestConsignmentWithSignatureRequest);

                client.Send(bytes);

                broker.WaitHandle.WaitOne();

                client.Shutdown(SocketShutdown.Both);
                client.Dispose();
            }
            finally
            {
                nanoHttp.Stop();
            }
            Assert.AreEqual(HttpVerb.Get, broker.Verb);
            Assert.AreEqual(Environment.NewLine == "\r\n" ? 13575 : 13515, broker.Body.Length);
            Assert.AreEqual(123, broker.Body[0]);
            Assert.AreEqual(Environment.NewLine == "\r\n" ? 34 : 46, broker.Body[1999]);
        }

        [TestMethod]
        public void GetClaimsFromSecurityToken_WithClaimContainingMultipleRequiredClaims_ReturnsValuesOfRequiredClaims()
        {
            
            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] {new Claim("claimtype1", "claim1value"), new Claim("claimtype2", "claim2value1"), new Claim("claimtype2", "claim2value2")})
            };

            List<string> claims = request.GetClaimsFromSecurityToken<string>("claimtype2").ToList();

            CollectionAssert.AreEquivalent(new[] { "claim2value1", "claim2value2" }, claims);
        }

        [TestMethod]
        public void GetClaimsFromSecurityToken_WithClaimContainingNoRequiredClaims_ReturnsEmptyCollection()
        {
            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(), Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("claimtype1", "claim1value"), new Claim("claimtype2", "claim2value1"), new Claim("claimtype2", "claim2value2") })
            };

            List<string> claims = request.GetClaimsFromSecurityToken<string>("claimtype0").ToList();

            CollectionAssert.AreEquivalent(new string[0], claims);
        }

        [TestMethod]
        public void UserId_WithClaimContainingUserIdAsUserId_ReturnsUserId()
        {
            var userId = Guid.NewGuid();

            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("userid", userId.ToString()), new Claim("claimtype1", "claim1value") })
            };

            Guid returnedUserId = request.UserId;

            Assert.AreEqual(userId, returnedUserId);
        }

        [TestMethod]
        public void UserId_WithClaimContainingUserIdAsSub_ReturnsUserId()
        {
            var userId = Guid.NewGuid();

            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("sub", userId.ToString()), new Claim("claimtype1", "claim1value") })
            };

            Guid returnedUserId = request.UserId;

            Assert.AreEqual(userId, returnedUserId);
        }

        [TestMethod]
        public void UserId_WithClaimNotContainingUserId_ThrowsException()
        {
            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("claimtype1", "claim1value") })
            };

            Assert.ThrowsException<InvalidOperationException>(() => request.UserId);
        }

        [TestMethod]
        public void OrganisationId_WithClaimContainingMultipleOrganisationClaims_ReturnsOrganisationIds()
        {
            var orgId1 = Guid.NewGuid();
            var orgId2 = Guid.NewGuid();

            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("claimtype1", "claim1value"), new Claim(ClaimTypeForOrganisationIds, orgId1.ToString()), new Claim(ClaimTypeForOrganisationIds, orgId2.ToString()) })
            };

            List<Guid> organisationIds = request.OrganisationIds.ToList();

            CollectionAssert.AreEquivalent(new[] { orgId1, orgId2 }, organisationIds);
        }

        [TestMethod]
        public void OrganisationId_WithClaimContainingNoOrganisationClaims_ReturnsEmptyCollection()
        {
            HttpRequest request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("claimtype1", "claim1value") })
            };

            List<Guid> organisationIds = request.OrganisationIds.ToList();

            CollectionAssert.AreEquivalent(new string[0], organisationIds);
        }

        [TestMethod]
        public void OrganisationIdAdministers_WithClaimContainingMultipleOrganisationAdministersClaimsAndOtherClaimTypes_ReturnsOrganisationIdsAdministers()
        {
            var orgAdministers1Id = Guid.NewGuid();
            var orgAdministers2Id = Guid.NewGuid();

            var claimsContainingTwoOrganisationIdsAdministers = new List<Claim>
            {
                new Claim("other claim type 1", "value"),
                new Claim(ClaimTypeForOrganisationIds, Guid.NewGuid().ToString()),
                new Claim(ClaimTypeForOrganisationIdsAdministers, orgAdministers1Id.ToString()),
                new Claim(ClaimTypeForOrganisationIdsAdministers, orgAdministers2Id.ToString())
            };
            
            var request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"), Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, claimsContainingTwoOrganisationIdsAdministers)
            };

            var organisationIdsAdministers = request.OrganisationIdsAdministers.ToList();

            Assert.AreEqual(2, organisationIdsAdministers.Count);
            CollectionAssert.AreEquivalent(new[] { orgAdministers1Id, orgAdministers2Id }, organisationIdsAdministers);
        }

        [TestMethod]
        public void OrganisationIdAdministers_WithClaimContainingNoOrganisationAdministersClaimsAndOtherClaimTypes_ReturnsEmptyCollection()
        {
            var claimsContainingTwoOrganisationIdsAdministers = new List<Claim>
            {
                new Claim("other claim type 1", "value"),
                new Claim(ClaimTypeForOrganisationIds, Guid.NewGuid().ToString())                
            };

            var request = new HttpRequest(Encoding.ASCII.GetBytes("GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\n\r\n"), Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, claimsContainingTwoOrganisationIdsAdministers)
            };

            var organisationIdsAdministers = request.OrganisationIdsAdministers.ToList();

            Assert.IsNotNull(organisationIdsAdministers);
            Assert.AreEqual(0, organisationIdsAdministers.Count);
        }

        [TestMethod]
        public void FinaliseLoad_WithvalidJwtRequiredEqualsFalse_ReadsTokenWithoutAuthentication()
        {
            var userIdInToken = new Guid("59c61cd1-27b9-4aed-bdf7-a93a1cc301d4");

            //bearerToken has been pre-encoded using JWT to contain the userId using (https://jwt.io/)
            var bearerToken = "eyJhbGciOiJIUzI1NiIsImtpZCI6ImIwODVmYzI0OTc3MDVhOTVjZTkzNjkyY2NmYjVjNzA2IiwidHlwIjoiSldUIn0.eyJzdWIiOiI1OWM2MWNkMS0yN2I5LTRhZWQtYmRmNy1hOTNhMWNjMzAxZDQifQ.IQpmgHteXZm3RE3d66HVcqYJbRWnOnASfCn7yn2D8GE";

            var request = new HttpRequest(Encoding.ASCII.GetBytes($"GET /description HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\nAuthorization: Bearer {bearerToken}\r\n"),
                Mock.Of<IConfigurationManager>(),Mock.Of<ILogger>())
            {
                SecurityToken = new JwtSecurityToken(null, null, new[] { new Claim("claimtype1", "claim1value") })
            };

            var tokenState = request.FinaliseLoad(false, new TokenValidationParameters());

            Assert.AreEqual(TokenState.Ok, tokenState);
            Assert.AreEqual(1, request.SecurityToken.Claims.Count());
            Assert.AreEqual(userIdInToken, request.UserId);
        }
    }

    public class MockRequestBroker : IRequestBroker
    {
        public byte[] Body { get; set; }
        public HttpVerb Verb { get; set; }

        public string Path { get; set; }

        public IHttpRequest ReceivedRequest { get; set; }

        public EventWaitHandle WaitHandle;

        public MockRequestBroker()
        {
            WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public IHttpResponse HandleRequest(IHttpRequest header, TokenState tokenState)
        {
            Body = header.Body;
            Path = header.Path;
            Verb = header.Verb;
            ReceivedRequest = header;

            this.WaitHandle.Set();

            Mock<IDateTimeProvider> dtp = new Mock<IDateTimeProvider>();
            dtp.Setup(p => p.UtcNow).Returns(DateTime.UtcNow);
            return new HttpResponse(dtp.Object, Mock.Of<ILogger>());
        }

        public int Precedence { get; } = 0;
    }
}
