using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.OrganisationHierarchyCache;
using StructureMap;

namespace Rook.Framework.Core.StructureMap.Registries
{
    public class BackplaneRegistry : Registry
    {
        public BackplaneRegistry()
        {
            ForConcreteType<RabbitBackplane>().Configure.Singleton();
            For<IBackplane>().Use(x => x.GetInstance<RabbitBackplane>());

            For<IBackplaneMessageHandler>().Use<BackplaneMessageHandler>();

            // These types consume messages off the backplane. 
            For<IBackplaneConsumer>().Add<MethodInspectorBackplaneConsumer>();
            For<IBackplaneConsumer>().Add(x => x.GetInstance<RequestStore>());
            For<IBackplaneConsumer>().Add(x => x.GetInstance<OrganisationHierarchyCacheItemBackplaneConsumer>());

            For<OrganisationHierarchyCacheItem>().Use<OrganisationHierarchyCacheItem>();

            For<MessageSubscriber.MethodInspector>().Use<MessageSubscriber.MethodInspector>();
            
            For<RequestMethodRegistration>().Use<RequestMethodRegistration>();

            For<BackplaneMessageHandler>().Use<BackplaneMessageHandler>();
        }
    }
}