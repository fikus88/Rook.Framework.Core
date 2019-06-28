﻿using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddHealthChecks();
			services.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
				.AddApplicationPart(Assembly.GetEntryAssembly());
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