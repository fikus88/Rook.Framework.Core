using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rook.Framework.Core.HttpServerAspNet;
using Rook.Framework.Core.HttpServerAspNet.ModelBinding;
using testService;

namespace Rook.Framework.Core.HttpServerAspNet
{
[Produces("application/json")]
[Route("testAttr/{IntroducerId}/somethingelse")]
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
	public string Post([FromHybrid] TestRequest req)
	{
		//req.IntroducerId = Guid.Parse(ControllerContext.RouteData.Values["IntroducerId"].ToString());
		
		return JsonConvert.SerializeObject(req);
	}
}
}