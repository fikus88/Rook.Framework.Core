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
		public Guid IntroducerId { get; set; }

		[HybridBindProperty(Source.Body)]
		public string Name { get; set; }

		public string Desc { get; set; }

		[HybridBindProperty(Source.QueryString)]
		[Required]
		public string Age { get; set; }

		[HybridBindProperty(Source.Header)]
		[Required]
		public string Key { get; set; }
	}
}