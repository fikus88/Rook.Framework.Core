using System;
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
        private readonly StartupOptions _startupOptions;
        private readonly AssemblyName _entryAssemblyName;
        private readonly ILogger _logger;
        private readonly IConfigurationManager _configurationManager;

        public Startup(IContainer container, StartupOptions startupOptions)
        {
	        _container = container;
	        _startupOptions = startupOptions;
	        _logger = _container.GetInstance<ILogger>();

	        _configurationManager = _container.GetInstance<IConfigurationManager>();
	        _startupOptions.IdentityServerOptions.Url = _configurationManager.Get<string>("IdentityServerAddress", null);

			var entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to get entry assembly");
	        _entryAssemblyName = entryAssembly.GetName();
        }

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");
			services.AddCustomMvc(_startupOptions);
			services.AddCustomAuthentication(_startupOptions);
			services.AddCustomAuthorization(_logger, _startupOptions);
            services.AddCustomCors(_startupOptions, _logger);
            services.AddSwagger(_entryAssemblyName, _startupOptions);

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

			var enableSubdomainCorsPolicy = _configurationManager.Get("EnableSubdomainCorsPolicy", false);
			var allowedSubdomainCorsPolicyOrigins = _configurationManager.Get("AllowedSubdomainCorsPolicyOrigins", string.Empty);

			_logger.Info($"{nameof(Startup)}.{nameof(Configure)}", new LogItem("EnableSubdomainCorsPolicy", enableSubdomainCorsPolicy.ToString()));
			if (enableSubdomainCorsPolicy)
			{
				_logger.Info($"{nameof(Startup)}.{nameof(Configure)}", new LogItem("AllowedSubDomainCorsPolicyOrigins", allowedSubdomainCorsPolicyOrigins));
				var allowedOriginsString = allowedSubdomainCorsPolicyOrigins;

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
			app.UseAuthentication();
			app.UseMvc();
		}
	}
}