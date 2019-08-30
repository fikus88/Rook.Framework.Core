using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HybridModelBinding;
using Microsoft.OpenApi.Models;
using Rook.Framework.Core.HttpServerAspNet;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Xunit;

namespace Rook.Framework.Core.Tests.Unit.Swashbuckle
{
	public class HybridOperationFilterTests
	{
		private class TestRequest1
		{
			[HybridBindProperty(Source.Route)] public Guid IntroducerId { get; set; }

			[HybridBindProperty(Source.Body)] public string Name { get; set; }

			public string Desc { get; set; }

			[HybridBindProperty(Source.QueryString)]
			[Required]
			public string Age { get; set; }

			[HybridBindProperty(Source.Header)]
			[Required]
			public string Key { get; set; }
		}

		private class TestRequest2
		{
			[HybridBindProperty(Source.Header)] public Guid IntroducerId { get; set; }

			public string Name { get; set; }

			public string Desc { get; set; }

			[HybridBindProperty(Source.QueryString)]
			[Required]
			public string Age { get; set; }

			[HybridBindProperty(Source.QueryString)]
			[Required]
			public string Key { get; set; }
		}

		private class TestRequest3
		{
			public Guid IntroducerId { get; set; }

			public string Name { get; set; }

			public string Desc { get; set; }

			public string Age { get; set; }

			public string Key { get; set; }
		}

		private class TestRequest4
		{
			[HybridBindProperty(Source.QueryString)]
			public Guid IntroducerId { get; set; }

			[HybridBindProperty(Source.QueryString)]
			public string Name { get; set; }

			[HybridBindProperty(Source.QueryString)]
			public string Desc { get; set; }

			[HybridBindProperty(Source.QueryString)]
			public string Age { get; set; }

			[HybridBindProperty(Source.QueryString)]
			public string Key { get; set; }
		}

		[Theory]
		[InlineData(typeof(TestRequest1), 1, true, 1, 1)]
		[InlineData(typeof(TestRequest2), 0, true, 2, 1)]
		[InlineData(typeof(TestRequest3), 0, true, 0, 0)]
		[InlineData(typeof(TestRequest4), 0, false, 5, 0)]
		public void ParametersSourcesAreValid(Type param, int routeParamsCount, bool bodyParamsRendered,
			int queryParamsCount, int headerParamsCount)
		{
			var filterContext = FilterContextFor(nameof(TestAttributeController.Post), param, out var operation);

			Subject().Apply(operation, filterContext);

			Assert.Equal(routeParamsCount, operation.Parameters.Count(x => x.In == ParameterLocation.Path));
			Assert.Equal(bodyParamsRendered, operation.RequestBody != null);
			Assert.Equal(queryParamsCount, operation.Parameters.Count(x => x.In == ParameterLocation.Query));
			Assert.Equal(headerParamsCount, operation.Parameters.Count(x => x.In == ParameterLocation.Header));
		}

		private OperationFilterContext FilterContextFor(string actionName, Type param,
			out OpenApiOperation operation)
		{
			var apiDescription = new ApiDescription
			{
				ActionDescriptor = new ControllerActionDescriptor
				{
					ControllerTypeInfo = typeof(TestAttributeController).GetTypeInfo(),
					MethodInfo = typeof(TestAttributeController).GetMethod(actionName)
				}
			};
			
			operation = new OpenApiOperation();

			apiDescription.ParameterDescriptions.Add(new ApiParameterDescription()
			{
				Source = new BindingSource("Hybrid", "Hybrid", false, true),
				Name = param.Name,
				Type = param
			});

			operation.Parameters.Insert(0, new OpenApiParameter()
			{
				Name = param.Name,
				Required = true
			});

			var context = new OperationFilterContext(
				apiDescription,
				new SchemaGenerator(new SchemaGeneratorOptions(), new JsonSerializerSettings()),
				new SchemaRepository(),
				(apiDescription.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo);

			return context;
		}

		private HybridOperationFilter Subject()
		{
			return new HybridOperationFilter();
		}
	}
}