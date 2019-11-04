using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    public sealed class HybridBindingSource : BindingSource
    {
        public HybridBindingSource()
            : base("Hybrid", "Hybrid", true, true)
        { }

        public override bool CanAcceptDataFrom(BindingSource bindingSource)
        {
            return bindingSource.Id == "Hybrid";
        }
    }
}