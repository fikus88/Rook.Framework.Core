using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Runtime.CompilerServices;
using HybridModelBinding;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.HttpServerAspNet
{
	/// <summary>
	/// Applies filter to schema of the Swagger documentation request body
	/// </summary>
	public class SwaggerIgnoreSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if (schema?.Properties == null || context == null)
				return;

			var excludedProperties = context.Type.GetProperties()
				.Where(t =>
					(t.GetCustomAttribute<HybridBindPropertyAttribute>() is HybridBindPropertyAttribute
						 customAttribute && customAttribute.ValueProviders.All(y => y != Source.Body)) ||
					t.GetCustomAttribute<SwaggerIgnoreAttribute>() != null);

			foreach (var excludedProperty in excludedProperties)
			{
				if (schema.Properties.ContainsKey(excludedProperty.Name.ToCamelCase())) ;
				schema.Properties.Remove(excludedProperty.Name.ToCamelCase());
			}
		}
	}
}