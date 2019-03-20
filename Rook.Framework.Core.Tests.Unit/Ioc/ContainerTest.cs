using System.Threading.Tasks;
using DummyMicroserviceCore;
using DummyMicroserviceCoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.IoC;

namespace Rook.Framework.Core.Tests.Unit.Ioc
{
    [TestClass]
    public class ContainerTest
    {
        [TestMethod, Description("BUG: TFS57920, Item with same key exists in dictionary.")]
        public void FindAttributedTypes_mutlithread_access_should_not_throw_exception()
        {
            Container.Scan();

            var task1 = new Task(() => Container.FindAttributedTypes<HandlerAttribute>());
            var task2 = new Task(() => Container.FindAttributedTypes<HandlerAttribute>());
            var task3 = new Task(() => Container.FindAttributedTypes<HandlerAttribute>());

            task1.Start();
            task2.Start();
            task3.Start();

            Task.WaitAll(task1, task2, task3);
        }

        [TestMethod]
        public void Scan_ScansAllReferencedAssemblies()
        {
            // Required to force compiler to recognise DummyMicroserviceCoreApi
            new DummyApiClass();

            Container.Scan(typeof(ContainerTest).Assembly);
            var result = Container.GetAllInstances<IInterface>();

            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void Scan_DoesNotBreakIfAssemblyScannedTwice()
        {
            // Required to force compiler to recognise DummyMicroserviceCoreApi
            new DummyApiClass();

            Container.Scan(typeof(ContainerTest).Assembly);
            Container.Scan(typeof(ContainerTest).Assembly);
            var result = Container.GetAllInstances<IInterface>();

            Assert.AreEqual(2, result.Length);
        }
    }
}
