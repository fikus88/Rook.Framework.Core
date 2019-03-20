using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Scaling;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.HttpServer;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.Tests.Unit.Structuremap
{
    [TestClass]
    public class StructureMapRegistryTests
    {
        [TestMethod]
        public void ContainerRegistry_IncludesRegistriesInEntryAssembly()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));

            Console.WriteLine(container.WhatDidIScan());

            var result = container.GetInstance<ITestInterfaceForStructureMapRegistry>();
            Assert.AreEqual(typeof(TestClassWhichWontBePickedUpByScanner), result.GetType());
        }

        [TestMethod]
        public void ContainerRegistry_ScanningInAnotherRegistryDoesNotBreakAnything()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));
            
            var result = container.GetInstance<ITestInterfaceWithGenericsForScan<string>>();
            Assert.AreEqual(typeof(TestClassWithGenericsForScan), result.GetType());
        }

        [TestMethod]
        public void ContainerRegistry_OverrideInEntryAssemblyRegistryWorks()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));

            var result = container.GetInstance<IRequestBroker>();
            Assert.AreEqual(typeof(OverrideTestRequestBroker), result.GetType());
        }

        [TestMethod]
        public void ContainerRegistry_CanPickUpRegistries_WhenTheyHaveAssemblyAsTheirFirstParameterInConstructor()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));

            var result = container.GetInstance<IRequestBroker>();

            //If this isn't the case, then UnitTestRegistry hasn't been picked up due to it's constructor
            Assert.AreEqual(typeof(OverrideTestRequestBroker), result.GetType());
        }

        [TestMethod]
        public void ContainerRegistry_StartableImplementationsAreSingletons()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));
            var startables = container.GetAllInstances<IStartable>().ToList();

            var isQueueWrapperSingleton = startables.Any(s => s.GetHashCode() == container.GetInstance<IQueueWrapper>().GetHashCode());
            var isServiceMetricSingleton = startables.Any(s => s.GetHashCode() == container.GetInstance<IServiceMetrics>().GetHashCode());
            var isMessageSubscriberSingleton = startables.Any(s => s.GetHashCode() == container.GetInstance<IMessageSubscriber>().GetHashCode());
            var isRabbitBackplaneSingleton = startables.Any(s => s.GetHashCode() == container.GetInstance<IBackplane>().GetHashCode());
            var isScalingMessagePublisher = startables.Any(s => s.GetHashCode() == container.GetInstance<ScalingMessagePublisher>().GetHashCode());
            var isNanoHttpSingleton = startables.Any(s => s.GetHashCode() == container.GetInstance<NanoHttp>().GetHashCode());
            var containsNoDuplicates = startables.Select(x => x.GetType().Name).Distinct().Count() == startables.Count;

            Assert.IsTrue(isQueueWrapperSingleton);
            Assert.IsTrue(isServiceMetricSingleton);
            Assert.IsTrue(isMessageSubscriberSingleton);
            Assert.IsTrue(isRabbitBackplaneSingleton);
            Assert.IsTrue(isScalingMessagePublisher);
            Assert.IsTrue(isNanoHttpSingleton);
            Assert.IsTrue(containsNoDuplicates);
        }

        [TestMethod]
        public void ContainerRegistry_HasSingletonInstanceOfRequestStoreWhenCallingGetAllInstances()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));
            var defaultInstance = container.GetInstance<IRequestStore>();
            var allInstance = container.GetAllInstances<IBackplaneConsumer>().Single(x => x is RequestStore);

            Assert.AreEqual(defaultInstance.GetType().GUID, allInstance.GetType().GUID);
        }

        [TestMethod]
        public void ContainerRegistry_HasSingletonInstanceOfService()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));
            var instance1 = container.GetInstance<IService>();
            var instance2 = container.GetInstance<IService>();

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }

        [TestMethod]
        public void ContainerRegistry_CreatesCorrectHttpResponse()
        {
            var container = new Container(new MicroserviceRegistry(typeof(StructureMapRegistryTests).Assembly));
            var response = container.GetInstance<IHttpResponse>();

            Assert.AreEqual(response.HttpStatusCode, HttpStatusCode.OK);
        }
    }

    public class UnitTestRegistry : Registry
    {
        public UnitTestRegistry(Assembly entryAssembly)
        {
            Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.ConnectImplementationsToTypesClosing(typeof(ITestInterfaceWithGenericsForScan<>));
            });

            For<ITestInterfaceForStructureMapRegistry>().Use<TestClassWhichWontBePickedUpByScanner>();
            For<IRequestBroker>().Use<OverrideTestRequestBroker>();
        }
    }

    public class TestClassWithGenericsForScan : ITestInterfaceWithGenericsForScan<string> { }

    public interface ITestInterfaceWithGenericsForScan<T> { }

    internal class TestClassWhichWontBePickedUpByScanner : ITestInterfaceForStructureMapRegistry { }

    public interface ITestInterfaceForStructureMapRegistry { }

    public class OverrideTestRequestBroker : IRequestBroker
    {
        public IHttpResponse HandleRequest(IHttpRequest header, TokenState tokenState)
        {
            throw new NotImplementedException();
        }

        public int Precedence { get; }
    };
}
