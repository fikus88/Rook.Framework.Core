using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.HttpServerAspNet
{
	internal class HeaderLoggingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

		public HeaderLoggingMiddleware(RequestDelegate next, ILogger logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			_logger.Trace($"{nameof(HeaderLoggingMiddleware)}.{nameof(InvokeAsync)}", new LogItem("Action", "Middleware Outputting Request Headers"));

			foreach (var header in context.Request.Headers)
			{
				_logger.Trace($"{nameof(HeaderLoggingMiddleware)}.{nameof(InvokeAsync)}", new LogItem("Action", "OutputHeader"), new LogItem(header.Key, header.Value));
			}

			_logger.Trace($"{nameof(HeaderLoggingMiddleware)}.{nameof(InvokeAsync)}", new LogItem("Action", "Middleware Request Header Output Complete"));

			await _next(context);

			_logger.Trace($"{nameof(HeaderLoggingMiddleware)}.{nameof(InvokeAsync)}", new LogItem("Action", "Middleware Outputting Response Headers"));

			foreach (var header in context.Response.Headers)
			{
				_logger.Trace("HttpContext.Response Header", new LogItem(header.Key, header.Value));
			}

			_logger.Trace($"{nameof(HeaderLoggingMiddleware)}.{nameof(InvokeAsync)}", new LogItem("Action", "Middleware Response Header Output Complete"));
		}
	}
}
