using Microsoft.AspNetCore.Mvc;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.HttpServerAspNet
{
	[Route("[controller]")]
	[ApiController]
	public class TestServiceController : ControllerBase
	{
		public TestServiceController(IConfigurationManager config)
		{
			
		}

		[HttpGet]
		public string GetHealthStatus()
		{
			return $"This is being returned from {nameof(TestServiceController)}";
		}
	}
}
