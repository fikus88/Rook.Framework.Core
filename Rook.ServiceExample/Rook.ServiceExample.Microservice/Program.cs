using System;
using System.Reflection;
using System.Threading;
using Microlise.MicroService.Core.IoC;
using Microlise.MicroService.Core.Services;

namespace Microlise.ServiceExample.Microservice
{
	class Program
	{
		static void Main(string[] args)
		{
#if NETCOREAPP2_0
			Container.Scan(Assembly.GetEntryAssembly(), typeof(IService).Assembly);
#else
            Container.Scan(Assembly.GetEntryAssembly(), typeof(IService).GetTypeInfo().Assembly);
#endif

			IService instance = Container.GetInstance<Service>();

			Thread.CurrentThread.Name = $"{instance.ServiceName} Main Thread";

			instance.Start();
#if NETCOREAPP2_0
			AppDomain.CurrentDomain.ProcessExit += (s, e) => instance.Stop();
#else

#endif
			Thread.CurrentThread.IsBackground = true;

			while (true)
				Thread.Sleep(int.MaxValue);
		}
	}
}
