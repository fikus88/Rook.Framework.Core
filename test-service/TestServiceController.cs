using Microsoft.AspNetCore.Mvc;

namespace Rook.Framework.Core.HttpServerAspNet
{
	[Route("[controller]")]
	[ApiController]
	public class TestServiceController : ControllerBase
	{
		[HttpGet]
		public string GetHealthStatus()
		{
			return $"This is being returned from {nameof(TestServiceController)}";
		}
	}
}
