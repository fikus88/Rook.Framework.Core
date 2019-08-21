using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

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
					t.GetCustomAttribute<SwaggerIgnoreAttribute>()
					!= null);

			foreach (var excludedProperty in excludedProperties)
			{
				var excludedPropCamelCaseNameArr = excludedProperty.Name.ToCharArray();
				var excludedPropCamelCaseName = "";
				
				for (int i = 0; i < excludedPropCamelCaseNameArr.Length; i++)
				{
					var currentChar = excludedPropCamelCaseNameArr[i].ToString();

					excludedPropCamelCaseName += i == 0 ? currentChar.ToLower() : currentChar;
				}

				if (schema.Properties.ContainsKey(excludedPropCamelCaseName)) ;
				schema.Properties.Remove(excludedPropCamelCaseName);
			}
		}
	}
}