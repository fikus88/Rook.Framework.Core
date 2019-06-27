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
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

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

			app.UseHttpsRedirection();

			//app.Use(async (context, next) =>
			//{
			//	if (context.Request.Path == "/health")
			//	{
			//		await context.Response.WriteAsync("All clear");
			//		return;
			//	}
			//	await next();
			//});

			app.UseMvc();
		}
	}
}