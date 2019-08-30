using System;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServerAspNet;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Rook.Framework.Core.Tests.Unit.Swashbuckle
{
	public class SwaggerIgnoreSchemaFilterTests
	{
		private class TestRequest1
		{
			[SwaggerIgnore] public string Param1 { get; set; }

			public string Param2 { get; set; }
		}

		private class TestRequest2
		{
			[SwaggerIgnore] public string Param1 { get; set; }
			[SwaggerIgnore] public string Param2 { get; set; }
		}

		private class TestRequest3
		{
			public string Param1 { get; set; }

			public string Param2 { get; set; }
		}

		[Theory]
		[InlineData(typeof(TestRequest1), 1)]
		[InlineData(typeof(TestRequest2), 0)]
		[InlineData(typeof(TestRequest3), 2)]
		public void ApplySwaggerIgnoreAttributeTest(Type type, int remainingPropertiesCount)
		{
			var context = FilterContextFor(type, out OpenApiSchema schema);

			Subject().Apply(schema, context);

			Assert.Equal(remainingPropertiesCount, schema.Properties.Count);
		}

		private SchemaFilterContext FilterContextFor(Type type, out OpenApiSchema schema)
		{
			var context = new SchemaFilterContext(type, null, new SchemaRepository(),
				new SchemaGenerator(new SchemaGeneratorOptions(), new JsonSerializerSettings()));

			schema = new OpenApiSchema();
			foreach (var prop in type.GetProperties())
			{
				schema.Properties.Add(prop.Name.ToCamelCase(), new OpenApiSchema());
			}

			return context;
		}

		private SwaggerIgnoreSchemaFilter Subject()
		{
			return new SwaggerIgnoreSchemaFilter();
		}
	}
}