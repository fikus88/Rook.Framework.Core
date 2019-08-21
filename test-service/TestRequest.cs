using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rook.Framework.Core.HttpServerAspNet;

namespace testService
{
	public class TestRequest
	{
		[FromRoute] [SwaggerIgnore] public Guid IntroducerId { get; set; }

		[FromBody] public string Name { get; set; }

		[FromBody] public string Desc { get; set; }
	}
}