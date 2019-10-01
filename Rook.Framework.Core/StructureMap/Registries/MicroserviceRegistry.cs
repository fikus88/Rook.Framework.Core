using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prometheus.Advanced;
using RabbitMQ.Client;
using Rook.Framework.Core.AnalyticsPump;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.Health;
using Rook.Framework.Core.HttpServer;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.OrganisationHierarchyCache;
using Rook.Framework.Core.Services;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Graph.Scanning;

namespace Rook.Framework.Core.StructureMap.Registries
{
    public class MicroserviceRegistry : Registry
    {
        public MicroserviceRegistry() : this(Assembly.GetEntryAssembly()) { }

        public MicroserviceRegistry(Assembly entryAssembly)
        {
            For<IContainerFacade>().Use<ContainerFacade>();

            var assemblies = GetAllReferencedAssemblies(entryAssembly);

            Scan(scan =>
            {
                assemblies.ForEach(scan.Assembly);
                scan.AssemblyContainingType<IConnectionFactory>();
                scan.ConnectImplementationsToTypesClosing(typeof(IMessageHandler2<,>));
                scan.AddAllTypesOf<IActivityHandler>();
                scan.AddAllTypesOf<IBackplaneConsumer>();
                scan.AddAllTypesOf<IHealthCheck>();

                scan.WithDefaultConventions();

                scan.With(new FindMicroserviceRegistries(entryAssembly));
            });

            IncludeRegistry<BackplaneRegistry>();
            IncludeRegistry<HttpRegistry>();
            IncludeRegistry<StartupRegistry>();

            For<IService>().Singleton().Use<Service>();

            For<MessageWrapper>().Use<MessageWrapper>();
            For<IRequestBroker>().Add<RequestBroker>();
            For<ICollectorRegistry>().Use(DefaultCollectorRegistry.Instance);

            ForConcreteType<RequestStore>().Configure.Singleton();
            For<IRequestStore>().Singleton().Use(x => x.GetInstance<RequestStore>());

            For<IOrganisationHierarchyCache>().Singleton().Use(x => x.GetInstance<OrganisationHierarchyCache.OrganisationHierarchyCache>());

            ExplicitlySetupInternalClasses();
        }


        private static List<Assembly> GetAllReferencedAssemblies(Assembly entryAssembly)
        {
            var fullNameCache = new List<string>();
            var assembliesToReturn = new List<Assembly> {entryAssembly};

            var assembliesToScan = new Stack<Assembly>();
            assembliesToScan.Push(entryAssembly);

            do
            {
                var assembly = assembliesToScan.Pop();

                var test = assembly.GetReferencedAssemblies().ToList();

                var referencedAssemblies = assembly.GetReferencedAssemblies()
                    .Where(x => !x.Name.Contains("StructureMap"))
                    .Where(x => !x.Name.Contains("VisualStudio"))
                    .Select(Assembly.Load)
                    .Where(a => !a.CodeBase.Contains(@"C:/Program Files/dotnet"))
                    .Where(a => a.GetTypes().Any(type => typeof(Registry).IsAssignableFrom(type)));

                foreach (var referencedAssembly in referencedAssemblies)
                {
                    if (!fullNameCache.Contains(referencedAssembly.FullName))
                    {
                        fullNameCache.Add(referencedAssembly.FullName);
                        assembliesToReturn.Add(referencedAssembly);
                        assembliesToScan.Push(referencedAssembly);
                    }
                }
            } while (assembliesToScan.Count > 0);
            
            return assembliesToReturn;
        }
        
        private void ExplicitlySetupInternalClasses()
        {
            ForConcreteType<MessageSubscriber>().Configure.Singleton();
            For<IMessageSubscriber>().Use(x => x.GetInstance<MessageSubscriber>());

            ForConcreteType<ServiceMetrics>().Configure.Singleton();
            For<IServiceMetrics>().Use(x => x.GetInstance<ServiceMetrics>());

            ForConcreteType<RabbitMqWrapper>().Configure.Singleton();
            For<IQueueWrapper>().Use(x => x.GetInstance<RabbitMqWrapper>());

            ForConcreteType<MessageHashes>().Configure.Singleton();

            For<IMethodInspectorManager>().Use<MethodInspectorManager>();
            For<ILogger>().Singleton().Use<AsyncLogger>();
            For<IConfigurationManager>().Singleton().Use<ConfigurationManager>();
            For<IRequestMatcher>().Singleton().Use<RequestMatcher>();

            For<IRabbitMqConnectionManager>().Singleton().Use<RabbitMqConnectionManager>();

            For<IMessageHashes>().Singleton().Use<MessageHashes>();

            For<IProducerWrapper>().Use<ProducerWrapper>();
        }
    }

    public class FindMicroserviceRegistries : IRegistrationConvention
    {
        private readonly IList<Type> _registryBlackList = new List<Type> { typeof(MicroserviceRegistry), typeof(HttpRegistry), typeof(StartupRegistry), typeof(BackplaneRegistry) };

        private readonly Assembly _entryAssembly;

        public FindMicroserviceRegistries(Assembly entryAssembly)
        {
            _entryAssembly = entryAssembly;
        }

        public void ScanTypes(TypeSet types, Registry registry)
        {
            var registries = types
                .FindTypes(TypeClassification.Closed | TypeClassification.Concretes)
                .Where(t => !_registryBlackList.Contains(t) && typeof(Registry).IsAssignableFrom(t));

            foreach (var reg in registries)
            {
                Registry instance;
                var ctorContainingAssmebly = reg.GetConstructor(new[] { typeof(Assembly) });
                if (ctorContainingAssmebly != null)
                {
                    instance = (Registry)ctorContainingAssmebly.Invoke(new object[] { _entryAssembly });
                }
                else
                {
                    instance = (Registry)Activator.CreateInstance(reg);
                }

                registry.IncludeRegistry(instance);
            }
        }
    }
}