using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Common;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
        private readonly IContainer _container;
		private IDictionary<string, CorsPolicy> _corsPolicies;
		private bool _enableSubdomainCorsPolicy;
		public const string _enableSubdomainCorsPolicyName = "EnableSubdomainCorsPolicy";

		public Startup(IContainer container)
        {
	        _container = container;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
	        _corsPolicies =_container.TryGetInstance<IDictionary<string, CorsPolicy>>() ?? new Dictionary<string, CorsPolicy>();
			var configurationManager = _container.GetInstance<IConfigurationManager>();

            services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");

			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
				.AddApplicationPart(Assembly.GetEntryAssembly());

            _enableSubdomainCorsPolicy = configurationManager.Get("EnableSubdomainCorsPolicy", false);
			
            if (!_enableSubdomainCorsPolicy)
            {
	            _corsPolicies.Add(_enableSubdomainCorsPolicyName,  new CorsPolicyBuilder().SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().Build());
            }

			services.AddCors(options =>
			{
				foreach (var corsPolicy in _corsPolicies)
				{
					options.AddPolicy(corsPolicy.Key, corsPolicy.Value);
				}
			});

			return ConfigureIoC(services, _container);
		}

		public IServiceProvider ConfigureIoC(IServiceCollection services, IContainer container)
		{
			container.Configure(config =>
			{
				config.Populate(services);
			});

			return container.GetInstance<IServiceProvider>();
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
				app.UseCors(_enableSubdomainCorsPolicyName);
			}
			
			app.UseHealthChecks("/health");
			app.UseHttpsRedirection();

			app.UseMvc();
		}
	}
}