﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class AspNetHttp : IStartStoppable
	{
		private readonly IConfigurationManager _configurationManager;
		private readonly ILogger _logger;
		private readonly IContainer _container;
		private readonly int port;
		private readonly int requestTimeout;
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		private CancellationToken _allocationCancellationToken;

		public StartupPriority StartupPriority => StartupPriority.Lowest;
		private IWebHost _webHost;

		public AspNetHttp(IConfigurationManager configurationManager, ILogger logger, IContainer container)
		{
			_configurationManager = configurationManager;
			_logger = logger;
			_container = container;

			const int defaultPort = 0;
			const int defaultRequestTimeout = 500;

			port = configurationManager.Get("Port", defaultPort);
			requestTimeout = configurationManager.Get("RequestTimeout", defaultRequestTimeout);
		}

		public void Start()
		{
			_allocationCancellationToken = cts.Token;
			Task.Run(RunWebHost, _allocationCancellationToken);
		}

		private void RunWebHost()
		{
			try
			{
				_logger.Info($"{nameof(AspNetHttp)}.{nameof(RunWebHost)}",
					new LogItem("Event", "Building ASP.NET Web Host"));
				_webHost = CreateWebHostBuilder().Build();
				_webHost.Run();
			}
			catch (Exception e)
			{
				_logger.Fatal($"{nameof(AspNetHttp)}.{nameof(RunWebHost)}",
					new LogItem("Event", "Unhandled exception"),
					new LogItem("Exception", e.Message),
					new LogItem("StackTrace", e.StackTrace));
			}
		}

		private IWebHostBuilder CreateWebHostBuilder()
		{
			var url = $"http://127.0.0.1:{port}";
			_logger.Info($"{nameof(AspNetHttp)}.{nameof(CreateWebHostBuilder)}", new LogItem("Event", "Running ASP.NET Web Host"), new LogItem("URL", url));
			return WebHost.CreateDefaultBuilder().UseStartup<Startup>().ConfigureServices((services) => services.AddSingleton(_container)).UseUrls(url);
		}

		public void Stop()
		{
			_logger.Info("Stopping ASP.NET Web Host");
			cts.Cancel();
			_webHost.StopAsync(_allocationCancellationToken);
			_logger.Info("ASP.NET Web Host Stopped");
		}
	}
}
