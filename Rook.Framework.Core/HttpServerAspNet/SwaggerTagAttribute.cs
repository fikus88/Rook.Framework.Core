using System;
using System.Collections.Generic;

namespace Rook.Framework.Core.HttpServerAspNet
{
	[AttributeUsage(AttributeTargets.Method)]
	public class SwaggerTagAttribute : Attribute
	{
		public IEnumerable<string> TagNames { get; }

		public SwaggerTagAttribute(params string[] tagNames)
		{
			TagNames = tagNames;
		}
	}
}