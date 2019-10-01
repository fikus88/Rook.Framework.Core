using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class StartupOptions
	{
		public FilterCollection Filters { get; } = new FilterCollection();
		public AuthorizationPolicyCollection AuthorizationPolicies { get; } = new AuthorizationPolicyCollection();
		public List<Type> SwaggerOperationFilters { get; } = new List<Type>();
		public List<Type> SwaggerSchemaFilters { get; } = new List<Type>();
		public IList<Type> AuthorizationHandlers { get; } = new List<Type>();
		public CorsPolicyCollection CorsPolicies { get; } = new CorsPolicyCollection();
		public IList<Assembly> MvcApplicationPartAssemblies { get; } = new List<Assembly> {Assembly.GetEntryAssembly()};
		public IdentityServerOptions IdentityServerOptions { get; } = new IdentityServerOptions();
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

	public class IdentityServerOptions
	{
		public bool RequireHttps { get; set; } = true;
		public string ValidAudience { get; set; }
		internal string Url { get; set; }
	}
}