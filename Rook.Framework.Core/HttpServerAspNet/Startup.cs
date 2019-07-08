using System;
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

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
        private readonly IContainer _container;
        private readonly IAspNetStartupConfiguration _aspNetStartupConfiguration;
        private readonly bool _enableSubdomainCorsPolicy;
		private readonly string _allowedSubdomainCorsPolicyOrigins;
        private readonly AssemblyName _entryAssemblyName;
        public static List<Assembly> MvcAssembliesToRegister { get; } = new List<Assembly> { Assembly.GetEntryAssembly() };

		public Startup(IContainer container)
        {
	        _container = container;
	        _aspNetStartupConfiguration = _container.TryGetInstance<IAspNetStartupConfiguration>();

	        var configurationManager = _container.GetInstance<IConfigurationManager>();
	        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Unable to get entry assembly");

			_enableSubdomainCorsPolicy = configurationManager.Get("EnableSubdomainCorsPolicy", false);
			_allowedSubdomainCorsPolicyOrigins = configurationManager.Get("_allowedSubdomainCorsPolicyOrigins", string.Empty);
	        _entryAssemblyName = entryAssembly.GetName();
        }

		public IServiceProvider ConfigureServices(IServiceCollection services)
        {
			services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");
            services.AddCustomMvc(MvcAssembliesToRegister, _aspNetStartupConfiguration != null 
	            ? _aspNetStartupConfiguration.ActionFilterTypes 
	            : Enumerable.Empty<Type>());
            services.AddCustomCors(_container);
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

			if (_enableSubdomainCorsPolicy)
			{
				app.UseCors(policy => policy.WithOrigins(_allowedSubdomainCorsPolicyOrigins).SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod());
			}
			
			app.UseHealthChecks("/health");
			app.UseHttpsRedirection();

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