using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.HttpServerAspNet;
using testService;

namespace Rook.Framework.Core.HttpServerAspNet
{
[Produces("application/json")]
[Route("test")]
[ApiController]
public class TestKinesisFirehoseController : ControllerBase
{

	private readonly IRequestStore _requestStore;

	public TestKinesisFirehoseController(IRequestStore requestStore)
	{
		_requestStore = requestStore;
	}
	
	
	/// <summary>
	/// Get the Health Status of the application
	/// </summary>
	/// <returns></returns>
	/// <response code="201">A string representation of the Health Status of the application</response>
	/// <response code="400">If a bad request is sent to the method</response>
	[HttpPost]
	[SwaggerTag("Kinesis Firehose")]
	public string Post(FirehoseDataSampleNeed req)
	{
		
		var msg =_requestStore.PublishAndWaitForTypedResponse(new Message<FirehoseDataSampleNeed, bool>()
		{
			Need = req,
			Method = "FirehoseKinesisTest"
		});
		
		
		return JsonConvert.SerializeObject(msg.Solution);
	}
}
}