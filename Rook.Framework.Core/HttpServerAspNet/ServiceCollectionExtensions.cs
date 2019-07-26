﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Rook.Framework.Core.Common;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	internal static class ServiceCollectionExtensions
	{
		internal static IMvcBuilder AddCustomMvc(this IServiceCollection services, StartupOptions startupOptions)
		{
			var mvcBuilder = services.AddMvc(options =>
				{
					foreach (var actionFilter in startupOptions.Filters)
					{
						options.Filters.Add(actionFilter);
					}
				})
				.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblies(startupOptions.MvcApplicationPartAssemblies))
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

			foreach (var assembly in startupOptions.MvcApplicationPartAssemblies)
			{
				mvcBuilder.AddApplicationPart(assembly);
			}

			services.AddRouting(options => options.LowercaseUrls = true);
			
			return mvcBuilder;
		}

		internal static AuthenticationBuilder AddCustomAuthentication(this IServiceCollection services)
		{
			// Disable default JWT claim mapping (see https://github.com/aspnet/AspNetCore/issues/4660)
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
				{
					options.Authority = "http://localhost:5000";
					options.RequireHttpsMetadata = false;
					options.Audience = "TestApi";
					options.TokenValidationParameters.RoleClaimType = "role";
				});
		}

		internal static IServiceCollection AddCustomAuthorization(this IServiceCollection services, ILogger logger,
			StartupOptions startupOptions)
		{
			services.AddAuthorization(configure =>
			{
				foreach (var authorizationPolicy in startupOptions.AuthorizationPolicies)
				{
					logger.Info($"{nameof(ServiceCollectionExtensions)}.{nameof(AddCustomMvc)}", 
						new LogItem("Event", $"Adding authorization policy. Name: '{authorizationPolicy.Key}', Policy: {JsonConvert.SerializeObject(authorizationPolicy.Value)}"));
					configure.AddPolicy(authorizationPolicy.Key, authorizationPolicy.Value);
				}
			});

			foreach (var authorizationHandler in startupOptions.AuthorizationHandlers)
			{
				services.AddSingleton(typeof(IAuthorizationHandler), authorizationHandler);
			}

			return services;
		}

		internal static IServiceCollection AddCustomCors(this IServiceCollection services, StartupOptions startupOptions, ILogger logger)
		{
	        logger.Info("AddCustomCors", new LogItem("FoundCustomCorsPolicies", startupOptions.CorsPolicies.Count));

	        services.AddCors(options =>
			{
				foreach (var corsPolicy in startupOptions.CorsPolicies)
				{
					options.AddPolicy(corsPolicy.Key, corsPolicy.Value);
					logger.Info("AddedCustomCorsPolicy", new LogItem("CorsPolicyKey", corsPolicy.Key),
						new LogItem("CorsPolicyValue", JsonConvert.SerializeObject(corsPolicy.Value)));
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
				c.OperationFilter<SecurityRequirementsOperationFilter>();
				c.SwaggerDoc("v1", new OpenApiInfo { Title = entryAssemblyName.Name, Version = entryAssemblyName.Version.ToString() });

				c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
				{
					Type = SecuritySchemeType.OAuth2,
					Flows = new OpenApiOAuthFlows()
					{
						ClientCredentials = new OpenApiOAuthFlow()
						{
							TokenUrl= new Uri("http://localhost:5000/connect/token"),
							Scopes = ImmutableDictionary<string, string>.Empty
						}
					}
				});
				
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
