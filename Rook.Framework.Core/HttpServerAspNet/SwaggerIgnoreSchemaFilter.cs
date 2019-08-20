using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class SwaggerIgnoreSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if (schema?.Properties == null || context == null)
				return;

			var excludedProperties = context.Type.GetProperties()
				.Where(t => 
					t.GetCustomAttribute<SwaggerIgnoreAttribute>() 
					!= null);

			foreach (var excludedProperty in excludedProperties)
			{
				if (schema.Properties.ContainsKey(excludedProperty.Name.ToLower()))
					schema.Properties.Remove(excludedProperty.Name.ToLower());
			}
		}
	}
}