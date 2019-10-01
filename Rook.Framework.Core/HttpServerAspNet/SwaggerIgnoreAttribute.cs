using System;

namespace Rook.Framework.Core.HttpServerAspNet
{
	/// <summary>
	/// Ignore property in request body
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class SwaggerIgnoreAttribute : Attribute
	{
	}
}