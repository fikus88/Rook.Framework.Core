using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Rook.Framework.Core.HttpServerAspNet
{
	[Produces("application/json")]
	[Route("[controller]")]
	[ApiController]
	public class TestServiceController : ControllerBase
	{
		/// <summary>
		/// Get the Health Status of the application
		/// </summary>
		/// <returns></returns>
		/// <response code="201">A string representation of the Health Status of the application</response>
		/// <response code="400">If a bad request is sent to the method</response>
		[HttpGet]
		[SwaggerTag("TEST")]
		public string AuthorizedGet()
		{
			return $"This is being returned from {nameof(TestServiceController)}";
		}
	}
}
