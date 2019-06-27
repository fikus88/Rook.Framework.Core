using System;
using System.Threading;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;

namespace test_service
{
    class Program
    {
		private static void Main()
		{
			var container = Bootstrapper.Init();
			IService instance = container.GetInstance<IService>();

			Thread.CurrentThread.Name = $"{ServiceInfo.Name} Main Thread";

			instance.Start();

			AppDomain.CurrentDomain.ProcessExit += (s, e) =>
			{
				instance.Stop();
			};

			Thread.CurrentThread.IsBackground = true;

			while (true)
				Thread.Sleep(int.MaxValue);
		}
	}
}
