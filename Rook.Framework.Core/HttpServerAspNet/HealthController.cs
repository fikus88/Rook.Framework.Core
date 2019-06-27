using Microsoft.AspNetCore.Mvc;

namespace Rook.Framework.Core.HttpServerAspNet
{
	[Route("[controller]")]
	[ApiController]
	public class HealthController : ControllerBase
	{
		[HttpGet]
		public string GetHealthStatus()
		{
			return "All clear Asp Net";
		}
	}
}
