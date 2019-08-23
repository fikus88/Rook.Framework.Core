using System;
using System.ComponentModel.DataAnnotations;
using HybridModelBinding;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rook.Framework.Core.HttpServerAspNet;

namespace testService
{
	public class TestRequest
	{
		[HybridBindProperty(Source.Route)]
		[SwaggerIgnore]
		public Guid IntroducerId { get; set; }

		[SwaggerIgnore] public string Name { get; set; }

		[SwaggerIgnore] public string Desc { get; set; }

		[HybridBindProperty(Source.QueryString)]
		[SwaggerIgnore]
		[Required]
		public string Age { get; set; }

		[HybridBindProperty(Source.Header)]
		[Required]
		[SwaggerIgnore]
		public string Key { get; set; }
	}
}