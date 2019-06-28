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
		readonly string SameDomainOrigin = "_sameDomainOrigin";
		string[] AllowedCorsOrigins;

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			var sp = services.BuildServiceProvider();
			var container = sp.GetRequiredService<IContainer>();

			services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");

			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
				.AddApplicationPart(Assembly.GetEntryAssembly());

			var configurationManager = container.GetInstance<IConfigurationManager>();
			AllowedCorsOrigins = configurationManager.Get<string>("AllowedCorsOrigins", null).Split(",");
			if (AllowedCorsOrigins != null)
			{
				services.AddCors(options =>
				{
					options.AddPolicy(SameDomainOrigin,
						builder =>
						{
							builder.WithOrigins(AllowedCorsOrigins);
							builder.SetIsOriginAllowedToAllowWildcardSubdomains();
						});
				});
			}

			return ConfigureIoC(services, container);
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

			if (AllowedCorsOrigins != null)
			{
				app.UseCors(SameDomainOrigin);
			}

			app.UseHealthChecks("/health");
			app.UseHttpsRedirection();

			app.UseMvc();
		}
	}
}