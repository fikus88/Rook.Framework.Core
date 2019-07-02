using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Rook.Framework.Core.HttpServerAspNet
{
	//[EnableCors("_allowedCorsOriginsPolicy")]
	[Route("[controller]")]
	[ApiController]
	public class TestServiceController : ControllerBase
	{
		public TestServiceController()
		{
		}

		[HttpGet]
		public string GetHealthStatus()
		{
			return $"This is being returned from {nameof(TestServiceController)}";
		}

		[HttpPost]
		public string PostTest()
		{
			return "PostTestSuccess";
		}
	}
}
