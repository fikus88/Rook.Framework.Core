using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class AspNetHttp : IStartStoppable
	{
		private readonly IConfigurationManager _configurationManager;
		private readonly ILogger _logger;
		private readonly int port;
		private readonly int requestTimeout;
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		private CancellationToken allocationCancellationToken;

		public StartupPriority StartupPriority => StartupPriority.Lowest;
		private IWebHost _webHost;

		public AspNetHttp(IConfigurationManager configurationManager, ILogger logger)
		{
			_configurationManager = configurationManager;
			_logger = logger;

			const int defaultPort = 8000;
			const int defaultRequestTimeout = 500;

			port = configurationManager.Get("Port", defaultPort);
			requestTimeout = configurationManager.Get("RequestTimeout", defaultRequestTimeout);
		}

		public void Start()
		{
			allocationCancellationToken = cts.Token;
			Task.Run(CreateWebHost, allocationCancellationToken);
		}

		private void CreateWebHost()
		{
			_webHost = CreateWebHostBuilder().Build();
			_webHost.Run();
		}

		private IWebHostBuilder CreateWebHostBuilder()
		{
			return WebHost.CreateDefaultBuilder().UseStartup<Startup>().UseUrls($"http://localhost:{port}");
		}

		public void Stop()
		{
			cts.Cancel();
			_webHost.StopAsync(allocationCancellationToken);
		}
	}
}
