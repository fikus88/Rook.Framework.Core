using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	internal static class ServiceCollectionExtensions
	{
		internal static IMvcBuilder AddCustomMvc(this IServiceCollection services, List<Assembly> mvcAssembliesToRegister, IEnumerable<Type> actionFilterTypes)
		{
			var mvcBuilder = services.AddMvc(options =>
				{
					foreach (var actionFilter in actionFilterTypes.Where(x => typeof(IFilterMetadata).IsAssignableFrom(x)))
					{
						Console.WriteLine($"Adding type {actionFilter.Name}");
						options.Filters.Add(actionFilter);
					}
				})
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

			foreach (var assembly in mvcAssembliesToRegister)
			{
				mvcBuilder.AddApplicationPart(assembly);
			}

			services.AddRouting(options => options.LowercaseUrls = true);

			return mvcBuilder;
		}

		internal static IServiceCollection AddCustomCors(this IServiceCollection services, IContainer container)
		{
	        var corsPolicies = container.TryGetInstance<IDictionary<string, CorsPolicy>>() ?? new Dictionary<string, CorsPolicy>();

	        services.AddCors(options =>
			{
				foreach (var corsPolicy in corsPolicies)
				{
					options.AddPolicy(corsPolicy.Key, corsPolicy.Value);
				}
			});

			return services;
		}

		internal static IServiceCollection AddSwagger(this IServiceCollection services, AssemblyName entryAssemblyName)
		{
			services.AddSwaggerGen(c =>
			{
				c.DocumentFilter<HealthCheckDocumentFilter>();
				c.OperationFilter<CustomTagOperationFilter>();
				c.SwaggerDoc("v1", new OpenApiInfo { Title = entryAssemblyName.Name, Version = entryAssemblyName.Version.ToString() });

				var xmlFile = $"{entryAssemblyName.Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

				if (File.Exists(xmlPath))
				{
					c.IncludeXmlComments(xmlPath);
				}
			});

			return services;
		}

		internal static IServiceProvider AddStructureMap(this IServiceCollection services, IContainer container)
		{
			container.Configure(config =>
			{
				config.Populate(services);
			});

			return container.GetInstance<IServiceProvider>();
		}
	}
}
