using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rook.Framework.Core.Application.Bus;
using StructureMap;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			var sp = services.BuildServiceProvider();
			var container = sp.GetRequiredService<IContainer>();

			services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbit_mq_health_check");

			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
				.AddApplicationPart(Assembly.GetEntryAssembly());

			return ConfigureIoC(services, container);;
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

			app.UseHealthChecks("/health");
			app.UseHttpsRedirection();

			app.UseMvc();
		}
	}
}