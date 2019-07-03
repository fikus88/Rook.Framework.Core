using System;
using System.Collections.Generic;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public interface IAspNetStartupConfiguration
	{
		IEnumerable<Type> ActionFilterTypes { get; }
	}
}
