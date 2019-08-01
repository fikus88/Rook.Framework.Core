using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;
using StructureMap;
using Microsoft.Extensions.Logging;
using ILogger = Rook.Framework.Core.Common.ILogger;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class AspNetHttp : IStartStoppable
	{
		private static readonly StartupOptions StartupOptions = new StartupOptions();
		private readonly ILogger _logger;
		private readonly IContainer _container;
		private readonly int port;
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		private CancellationToken _allocationCancellationToken;

		public StartupPriority StartupPriority => StartupPriority.Lowest;
		private IWebHost _webHost;

		public AspNetHttp(IConfigurationManager configurationManager, ILogger logger, IContainer container)
		{
			_logger = logger;
			_container = container;

			const int defaultPort = 0;
			//const int defaultRequestTimeout = 500;

			port = configurationManager.Get("Port", defaultPort);
			//requestTimeout = configurationManager.Get("RequestTimeout", defaultRequestTimeout);
		}

		public static void Configure(Action<StartupOptions> configure)
		{
			configure(StartupOptions);
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
				_webHost.Start();

				foreach (var address in _webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses)
				{
					var url = new Uri(address);
					_logger.Info($"{nameof(AspNetHttp)}.{nameof(RunWebHost)}",
						new LogItem("Event", "WebHost started"), new LogItem("Url", url.ToString()));
				}

				_webHost.WaitForShutdown();
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
			var url = $"http://*:{port}";
			return WebHost.CreateDefaultBuilder()
				.ConfigureLogging((logging) => logging.ClearProviders())
				.UseStartup<Startup>()
				.ConfigureServices((services) =>
				{
					services.AddSingleton(_container);
					services.AddSingleton(StartupOptions);
				})
				.UseUrls(url);
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