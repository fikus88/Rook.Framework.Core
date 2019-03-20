using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Health;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit.Health
{
    [TestClass]
    public class HealthCheckTests
    {
        [TestMethod]
        public void AllHealthChecksPickedUpByContainer()
        {
            var container = new Container(new MicroserviceRegistry(typeof(HealthCheckTests).Assembly));

            var results = container.GetAllInstances(typeof(IHealthCheck)).Cast<IHealthCheck>();

            Assert.AreEqual(2, results.Count());
        }
    }

    public class TestHealthCheck : IHealthCheck
    {
        public bool IsHealthy()
        {
            return true;
        }
    }
}
