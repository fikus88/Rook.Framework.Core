using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Common;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
        private readonly IContainer _container;
        private readonly bool _enableSubdomainCorsPolicy;
        private readonly AssemblyName _entryAssemblyName;
        public static List<Assembly> MvcAssembliesToRegister { get; } = new List<Assembly> { Assembly.GetEntryAssembly() };

		public Startup(IContainer container)
        {
	        _container = container;
	        var configurationManager = _container.GetInstance<IConfigurationManager>();
	        _enableSubdomainCorsPolicy = configurationManager.Get("EnableSubdomainCorsPolicy", false);
	        _entryAssemblyName = Assembly.GetEntryAssembly()?.GetName() ?? new AssemblyName("Unknown") { Version = new Version(1, 0) };

        }

		public IServiceProvider ConfigureServices(IServiceCollection services)
        {
			services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");
            services.AddCustomMvc(MvcAssembliesToRegister);
            services.AddCustomCors(_container);
            services.AddSwagger(_entryAssemblyName);
			return services.AddStructureMap(_container);
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
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
				app.UseCors(policy => policy.SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod());
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