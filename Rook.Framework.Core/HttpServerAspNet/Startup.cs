﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Common;
using StructureMap;
using ILogger = Rook.Framework.Core.Common.ILogger;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
        private readonly IContainer _container;
        private readonly IAspNetStartupConfiguration _aspNetStartupConfiguration;
        private readonly bool _enableSubdomainCorsPolicy;
		private readonly string _allowedSubdomainCorsPolicyOrigins;
        private readonly AssemblyName _entryAssemblyName;
        private readonly ILogger _logger;

        public static List<Assembly> MvcAssembliesToRegister { get; } = new List<Assembly> { Assembly.GetEntryAssembly() };

		public Startup(IContainer container)
        {
	        _container = container;
	        _aspNetStartupConfiguration = _container.TryGetInstance<IAspNetStartupConfiguration>();
	        _logger = _container.GetInstance<ILogger>();

	        var configurationManager = _container.GetInstance<IConfigurationManager>();
	        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to get entry assembly");

			_enableSubdomainCorsPolicy = configurationManager.Get("EnableSubdomainCorsPolicy", false);
			_allowedSubdomainCorsPolicyOrigins = configurationManager.Get("AllowedSubdomainCorsPolicyOrigins", string.Empty);
	        _entryAssemblyName = entryAssembly.GetName();
        }

		public IServiceProvider ConfigureServices(IServiceCollection services)
        {
			services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");
			services.AddCustomCors(_container, _logger);
            services.AddCustomMvc(MvcAssembliesToRegister, _aspNetStartupConfiguration != null 
	            ? _aspNetStartupConfiguration.ActionFilterTypes 
	            : Enumerable.Empty<Type>());
            services.AddSwagger(_entryAssemblyName);
			return services.AddStructureMap(_container);
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddProvider(new RookLoggerProvider(_container));
			
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
			}

			_logger.Info("Configure", new LogItem("EnableSubdomainCorsPolicy", _enableSubdomainCorsPolicy.ToString()));
			if (_enableSubdomainCorsPolicy)
			{
				_logger.Info("Configure", new LogItem("AllowedSubDomainCorsPolicyOrigins", _allowedSubdomainCorsPolicyOrigins));
				var allowedOriginsString = _allowedSubdomainCorsPolicyOrigins;

				var allowedOrigins = allowedOriginsString.Split(';');

				app.UseCors(policy => policy.WithOrigins(allowedOrigins).SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod());
			}
			
			app.UseHealthChecks("/health");
			app.UseHttpsRedirection();

			app.Use(async (context, next) =>
			{
				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Initiated"));

				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Outputting Request Headers"));

				foreach (var header in context.Request.Headers)
				{
					_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "OutputHeader"), new LogItem(header.Key, header.Value));
				}

				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Header Output Complete"));

				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Pipeline Begin"));

				await next.Invoke();

				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Pipeline Complete"));

				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Outputting Response Headers"));

				foreach (var header in context.Response.Headers)
				{
					_logger.Trace("HttpContext.Response Header", new LogItem(header.Key, header.Value));
				}

				_logger.Trace(typeof(Startup) + ".Configure()", new LogItem("Action", "Middleware Header Output Complete"));
			});

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.RoutePrefix = "";
				c.SwaggerEndpoint("/swagger/v1/swagger.json", _entryAssemblyName.Version.ToString());
			});

			app.UseMvc();
		}
	}
}