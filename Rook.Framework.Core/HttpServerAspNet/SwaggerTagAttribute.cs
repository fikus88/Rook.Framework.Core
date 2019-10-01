using System;
using System.Collections.Generic;

namespace Rook.Framework.Core.HttpServerAspNet
{
    /// <summary>
    /// Ignore property in request body
    /// </summary>
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