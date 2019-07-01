using System;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Scaling;
using Rook.Framework.Core.Application.Subscribe;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;
using Rook.Framework.Core.HttpServerAspNet;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;
using StructureMap;

namespace Rook.Framework.Core.StructureMap.Registries
{
    public class StartupRegistry : Registry
    {
	    public StartupRegistry()
	    {
		    var configurationContainer = new Container(new ConfigurationRegistry());
		    var configurationManager = configurationContainer.GetInstance<IConfigurationManager>();

		    if (!Enum.TryParse(configurationManager.Get<string>("HttpServerType", "AspNetHttp"), out HttpServerType httpServerType))
		    {
			    throw new ArgumentException("Invalid value provided for HttpServerType in configuration");
		    }

		    switch(httpServerType)
		    {
				case HttpServerType.NanoHttp:
					ForConcreteType<NanoHttp>().Configure.Singleton();
					For<IStartable>().Singleton().Add(x => x.GetInstance<NanoHttp>());
					For<IStartStoppable>().Singleton().Add(x => x.GetInstance<NanoHttp>());
					break;
				default:
					ForConcreteType<AspNetHttp>().Configure.Singleton();
					For<IStartable>().Singleton().Add(x => x.GetInstance<AspNetHttp>());
					For<IStartStoppable>().Singleton().Add(x => x.GetInstance<AspNetHttp>());
					break;
		    }

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
