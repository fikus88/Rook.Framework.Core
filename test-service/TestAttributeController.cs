using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rook.Framework.Core.HttpServerAspNet;
using testService;

namespace Rook.Framework.Core.HttpServerAspNet
{
[Produces("application/json")]
[Route("testAttr/{Id}")]
[ApiController]
public class TestAttributeController : ControllerBase
{
	/// <summary>
	/// Get the Health Status of the application
	/// </summary>
	/// <returns></returns>
	/// <response code="201">A string representation of the Health Status of the application</response>
	/// <response code="400">If a bad request is sent to the method</response>
	[HttpPost]
	[SwaggerTag("TEST Attribute")]
	public string Post(TestRequest req)
	{
		req.Id = int.Parse(ControllerContext.RouteData.Values["Id"].ToString());
		
		return JsonConvert.SerializeObject(req);
	}
}
}