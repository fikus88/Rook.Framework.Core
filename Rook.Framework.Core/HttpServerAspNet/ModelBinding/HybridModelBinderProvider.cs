using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    public abstract class HybridModelBinderProvider : IModelBinderProvider
    {
        public HybridModelBinderProvider(
            BindingSource bindingSource,
            IModelBinder modelBinder)
        {
            if (bindingSource == null)
            {
                throw new ArgumentNullException(nameof(bindingSource));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            this.bindingSource = bindingSource;
            this.modelBinder = modelBinder;
        }

        private BindingSource bindingSource { get; set; }
        private IModelBinder modelBinder { get; set; }

        public virtual IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.BindingInfo?.BindingSource != null &&
                context.BindingInfo.BindingSource.CanAcceptDataFrom(bindingSource))
            {
                return modelBinder;
            }
            else
            {
                return null;
            }
        }
    }
}