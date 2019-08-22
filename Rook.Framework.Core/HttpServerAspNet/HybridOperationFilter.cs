using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using HybridModelBinding;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class HybridOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			var hybridParameter = context.ApiDescription.ParameterDescriptions
				.Where(x => x.Source.Id == "Hybrid")
				.Select(x => new
				{
					name = x.Name,
					schema = context.SchemaGenerator.GenerateSchema(x.Type, context.SchemaRepository),
					type = x.Type
				}).ToList().FirstOrDefault();

			var paramsHandled = hybridParameter?.type.GetProperties().Length;

			for (var i = 0; i < operation.Parameters.Count; i++)
			{
				if (hybridParameter != null && hybridParameter.name == operation.Parameters[i].Name)
				{
					var name = operation.Parameters[i].Name;
					var isRequired = operation.Parameters[i].Required;

					operation.Parameters.Clear();

					foreach (var propertyInfo in hybridParameter.type.GetProperties())
					{
						var paramSet = false;

						foreach (var attribute in propertyInfo.GetCustomAttributes(typeof(HybridBindPropertyAttribute))
							.ToList())
						{
							var thisPropSchema = context.SchemaGenerator.GenerateSchema(propertyInfo.PropertyType,
								context.SchemaRepository);

							var apiParam = new OpenApiParameter()
							{
								Description = propertyInfo.Name,
								Required = propertyInfo.GetCustomAttributes(typeof(RequiredAttribute)).ToList()
									           .Count() == 1,
								Schema = thisPropSchema,
								Name = propertyInfo.Name
							};

							switch (((HybridBindPropertyAttribute) attribute).ValueProviders[0])
							{
								case Source.QueryString:
									apiParam.In = ParameterLocation.Query;
									break;
								case Source.Header:
									apiParam.In = ParameterLocation.Header;
									break;
								case Source.Route:
									apiParam.In = ParameterLocation.Path;
									break;
							}

							operation.Parameters.Insert(0, apiParam);
							paramsHandled--;
							paramSet = true;
						}

						if (!paramSet)
							paramsHandled -=
								!propertyInfo.GetCustomAttributes(typeof(SwaggerIgnoreAttribute)).Any() ? 0 : 1;
					}

					if (paramsHandled != 0)
					{
						operation.RequestBody = new OpenApiRequestBody()
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								["application/json"] = new OpenApiMediaType
								{
									Schema = context.SchemaRepository.Schemas[hybridParameter.type.Name]
								}
							},
							Description = "Request Body",
							Required = true
						};
					}
				}
			}
		}
	}
}