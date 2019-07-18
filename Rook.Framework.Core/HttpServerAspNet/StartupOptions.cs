using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class StartupOptions
	{
		public IList<Type> Filters { get; } = new List<Type>();
		public AuthorizationPolicyCollection AuthorizationPolicies { get; } = new AuthorizationPolicyCollection();
		public IList<Type> AuthorizationHandlers { get; } = new List<Type>();
		public CorsPolicyCollection CorsPolicies { get; } = new CorsPolicyCollection();
		public IList<Assembly> MvcApplicationPartAssemblies{ get; } = new List<Assembly> { Assembly.GetEntryAssembly() };
	}

	public class AuthorizationPolicyCollection : Dictionary<string, AuthorizationPolicy>
	{
		public void Add(string policyName, Action<AuthorizationPolicyBuilder> configurePolicy)
		{
			var builder = new AuthorizationPolicyBuilder(Array.Empty<string>());
			configurePolicy(builder);
			Add(policyName, builder.Build());
		}
	}

	public class CorsPolicyCollection : Dictionary<string, CorsPolicy>
	{
		public void Add(string policyName, Action<CorsPolicyBuilder> configurePolicy)
		{
			var builder = new CorsPolicyBuilder(Array.Empty<string>());
			configurePolicy(builder);
			Add(policyName, builder.Build());
		}
	}
}
