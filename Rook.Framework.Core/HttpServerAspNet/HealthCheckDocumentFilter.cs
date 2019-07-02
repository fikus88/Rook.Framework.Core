using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rook.Framework.Core.HttpServerAspNet
{
	class HealthCheckDocumentFilter : IDocumentFilter
	{
		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			swaggerDoc.Paths.Add("/health", new OpenApiPathItem()
			{
				Operations = new Dictionary<OperationType, OpenApiOperation>()
				{
					{ OperationType.Get, new OpenApiOperation
					{
						Description = "Returns the health of the service",
						Summary = "Health check",
						OperationId = "health",
						Responses = new OpenApiResponses()
						{
							{ "200", new OpenApiResponse() { Description = "The health of the service; Healthy or Degraded" } },
							{ "503", new OpenApiResponse() {  Description = "The health of the service; Unhealthy" }
						} },
						Tags = new List<OpenApiTag> { new OpenApiTag { Name = "System" } }
					} }
				}
			});
		}
	}
}
