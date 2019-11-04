using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromHybridAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource => new HybridBindingSource();
    }
}