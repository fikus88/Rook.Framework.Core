using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class RequestResponseLoggingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

		public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger logger)
		{
			_next = next;
			_logger = logger;
		}

	    public async Task Invoke(HttpContext context)
	    {
	        //Copy a pointer to the original response body stream
	        var originalBodyStream = context.Response.Body;

	        //Create a new memory stream...
	        using (var responseBody = new MemoryStream())
	        {
	            //...and use that for the temporary response body
	            context.Response.Body = responseBody;

	            //Continue down the Middleware pipeline, eventually returning to this class
	            await _next(context);

				// Log the Header
				foreach(var header in context.Response.Headers)
				{
					_logger.Trace("HttpContext.Response Header", new LogItem(header.Key, header.Value));
				}
				
	            //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
	            await responseBody.CopyToAsync(originalBodyStream);
	        }
	    }
	}
}
