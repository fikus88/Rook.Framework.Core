using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

		public ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
		{
			_logger = logger;
			_next = next;
		}

		public async Task InvokeAsync(HttpContext httpContext)
		{
			try
			{
				await _next(httpContext);
			}
			catch (Exception ex)
			{
				_logger.Exception(ex.TargetSite.ReflectedType?.FullName ?? httpContext.Request.Path.Value, "Unhandled exception occured.", ex);
				await HandleExceptionAsync(httpContext);
			}
		}

		private Task HandleExceptionAsync(HttpContext context)
		{
			context.Response.ContentType = "application/json";
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			return context.Response.WriteAsync(new
			{
				context.Response.StatusCode,
				Message = "Internal Server Error"
			}.ToString());
		}
	}
}
