using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public interface IAspNetStartupConfiguration
	{
		IEnumerable<IActionFilter> ActionFilters { get; }
		IEnumerable<Type> ActionFilterTypes { get; }
		void AddMiddleware(IApplicationBuilder applicationBuilder);
	}
}
