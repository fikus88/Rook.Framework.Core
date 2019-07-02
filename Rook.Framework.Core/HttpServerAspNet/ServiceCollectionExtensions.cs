﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	internal static class ServiceCollectionExtensions
	{
		internal static IMvcBuilder AddCustomMvc(this IServiceCollection services, List<Assembly> mvcAssembliesToRegister)
		{
			var mvcBuilder = services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

			foreach (var assembly in mvcAssembliesToRegister)
			{
				mvcBuilder.AddApplicationPart(assembly);
			}

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
				c.SwaggerDoc("v1", new OpenApiInfo { Title = entryAssemblyName?.Name, Version = entryAssemblyName?.Version.ToString() });
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