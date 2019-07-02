using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class CustomTagOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			var swaggerTagAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
				.Union(context.MethodInfo.GetCustomAttributes(true))
				.OfType<SwaggerTagAttribute>();

			operation.Tags = swaggerTagAttributes.SelectMany(x => x.TagNames).Select(x => new OpenApiTag { Name = x }).ToList();
		}
	}
}
