using System;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Prometheus.Advanced;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;

namespace Rook.Framework.Core.HttpServerAspNet
{
	[ApiController]
	[Route("[controller]")]
	public class MetricsController : ControllerBase
	{
		private readonly ICollectorRegistry _registry;
		private readonly ILogger _logger;

		public MetricsController(ICollectorRegistry registry, ILogger logger)
		{
			_registry = registry;
			_logger = logger;
		}

		[HttpGet]
		[SwaggerTag("System")]
		public IActionResult Index()
		{
			try
			{
				var metrics = _registry.CollectAll().ToArray();

				_logger.Trace($"{nameof(MetricsController)}.{nameof(Handle)}",
					new LogItem("Event", "CollectAll result"),
					new LogItem("MetricsCount", metrics.Length));

				var acceptHeader = Request.Headers["Accept"];
				var acceptHeaders = acceptHeader.ToArray();

				var contentType = ScrapeHandler.GetContentType(acceptHeaders);

				var metricsContent = new MetricsHttpContent(_logger, metrics, contentType);
				Response.StatusCode = (int) HttpStatusCode.OK;
				Response.ContentType = contentType;
				metricsContent.WriteToStream(Response.Body);

				return new EmptyResult();
			}
			catch (Exception ex)
			{
				_logger.Error($"{nameof(MetricsController)}.{nameof(Handle)}",
					new LogItem("Event", "ScrapeFailed"),
					new LogItem("Exception", ex.Message),
					new LogItem("StackTrace", ex.StackTrace));


				return StatusCode((int)HttpStatusCode.ServiceUnavailable, ex.Message);
			}
		}
	}
}