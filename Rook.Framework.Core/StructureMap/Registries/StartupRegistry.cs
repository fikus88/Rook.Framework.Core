using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Scaling;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.HttpServer;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;
using StructureMap;

namespace Rook.Framework.Core.StructureMap.Registries
{
    public class StartupRegistry : Registry
    {
        public StartupRegistry()
        {
	        ForConcreteType<AspNetHttp>().Configure.Singleton();
			For<IStartable>().Singleton().Add(x => x.GetInstance<AspNetHttp>());
			For<IStartStoppable>().Singleton().Add(x => x.GetInstance<AspNetHttp>());

			For<IStartable>().Add(x => x.GetInstance<ServiceMetrics>());

            For<IStartable>().Add(x => x.GetInstance<MessageSubscriber>());
            For<IStartStoppable>().Add(x => x.GetInstance<MessageSubscriber>());

            For<IStartable>().Add(x => x.GetInstance<RabbitMqWrapper>());
            For<IStartStoppable>().Add(x => x.GetInstance<RabbitMqWrapper>());

            ForConcreteType<ScalingMessagePublisher>().Configure.Singleton();
            For<IStartable>().Add(x => x.GetInstance<ScalingMessagePublisher>());
            For<IStartStoppable>().Add(x => x.GetInstance<ScalingMessagePublisher>());

            For<IStartable>().Add(x => x.GetInstance<RabbitBackplane>());
            For<IStartStoppable>().Add(x => x.GetInstance<RabbitBackplane>());
        }
    }
}
