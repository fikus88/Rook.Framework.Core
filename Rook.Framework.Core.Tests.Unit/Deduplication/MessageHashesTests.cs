using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit.Deduplication
{
    [TestClass]
    public class MessageHashesTests
    {
        [TestMethod]
        public void Container_HasSingletonInstance()
        {
            var container = new Container(new MicroserviceRegistry(typeof(MessageHashesTests).Assembly));
            var instance1 = container.GetInstance<MessageHashes>();
            var instance2 = container.GetInstance<MessageHashes>();

            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void Container_InjectsSingletonInstancesIntoClasses()
        {
            var container = new Container(new MicroserviceRegistry(typeof(MessageHashesTests).Assembly));
            var instance1 = container.GetInstance<MethodInspectorBackplaneConsumer>()._receivedHashes;
            var instance2 = container.GetInstance<MethodInspectorManager>()._receivedHashes;

            Assert.AreSame(instance1, instance2);
        }
    }
}
