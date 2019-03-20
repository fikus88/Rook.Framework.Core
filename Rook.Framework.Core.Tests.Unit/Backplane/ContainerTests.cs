using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.OrganisationHierarchyCache;
using Rook.Framework.Core.StructureMap;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit.Backplane
{
    [TestClass]
    public class ContainerTests
    {
        [TestMethod]
        public void Container_GetsAllInstancesOfBackplane()
        {
            var container = new Container(new MicroserviceRegistry(typeof(ContainerTests).Assembly));
            var result = container.GetAllInstances<IBackplaneConsumer>().ToList();

            var containsTest = result.Any(x => x is TestBackplaneConsumer);
            var containsRequestStore = result.Any(x => x is RequestStore);
            var containsRequestMethodRegistration = result.Any(x => x is RequestMethodRegistrationConsumer);
            var containsMethodInspector = result.Any(x => x is MethodInspectorBackplaneConsumer);
            var containsOrganisationHierarchyCacheItem = result.Any(x => x is OrganisationHierarchyCacheItemBackplaneConsumer);
            var containsNoDuplicates = result.Select(x => x.GetType().Name).Distinct().Count() == result.Count;

            Assert.IsTrue(containsTest);
            Assert.IsTrue(containsRequestStore);
            Assert.IsTrue(containsRequestMethodRegistration);
            Assert.IsTrue(containsMethodInspector);
            Assert.IsTrue(containsOrganisationHierarchyCacheItem);
            Assert.IsTrue(containsNoDuplicates);
        }

        [TestMethod]
        public void Container_CanConstructRabbitBackplane()
        {
            var container = new Container(new MicroserviceRegistry(typeof(ContainerTests).Assembly));
            var result = container.GetInstance<IBackplane>();

            Assert.IsTrue(result is RabbitBackplane);
        }
    }

    [TestClass]
    public class LegacyContainerTests
    {
        [ClassInitialize]
        public static void BeforeAllTests(TestContext testContext)
        {
            IoC.Container.Map<IContainerFacade>(new ContainerFacade(null, IoC.Container.GetInstance<IConfigurationManager>()));
            IoC.Container.Scan(typeof(LegacyContainerTests).Assembly);
        }

        [TestMethod]
        public void LegacyContainer_CanBuildRabbitBackplane()
        {
            var backplane = IoC.Container.GetInstance<IBackplane>();

            Assert.IsTrue(backplane is RabbitBackplane);
        }

        [TestMethod]
        public void LegacyContainer_CanBuildMethodInspectorManager()
        {
            var result = IoC.Container.GetInstance<IMethodInspectorManager>();

            Assert.IsTrue(result is MethodInspectorManager);
        }
    }
    
    public class TestBackplaneConsumer : BackplaneConsumer<Guid>
    {
        public override void Consume(Guid value)
        {
        }
    }
}
