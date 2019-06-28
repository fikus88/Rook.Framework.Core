using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Common;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
        private const string AllowedCorsOriginsPolicy = "_allowedCorsOriginsPolicy";
        string[] _allowedCorsOrigins;

        private readonly IContainer _container;

        public Startup(IContainer container)
        {
            _container = container;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
		{
            services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");

			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
				.AddApplicationPart(Assembly.GetEntryAssembly());

			var configurationManager = _container.GetInstance<IConfigurationManager>();
            _allowedCorsOrigins = configurationManager.Get<string>("AllowedCorsOrigins", null).Split(",");

			if (_allowedCorsOrigins != null)
            { 
                services.AddCors(options =>
				{
					options.AddPolicy(AllowedCorsOriginsPolicy,
						builder =>
						{
							builder.WithOrigins(_allowedCorsOrigins);
							builder.SetIsOriginAllowedToAllowWildcardSubdomains();
						});
				});
			}

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

			if (_allowedCorsOrigins != null)
			{
				app.UseCors(AllowedCorsOriginsPolicy);
			}

			app.UseHealthChecks("/health");
			app.UseHttpsRedirection();

			app.UseMvc();
		}
	}
}