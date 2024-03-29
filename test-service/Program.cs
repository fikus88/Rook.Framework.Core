﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServerAspNet;
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

			//AspNetHttp.Configure(options =>
			//{
			//	options.CorsPolicies.Add("_allowedCorsOriginsPolicy", policy => policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod());
			//});

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
