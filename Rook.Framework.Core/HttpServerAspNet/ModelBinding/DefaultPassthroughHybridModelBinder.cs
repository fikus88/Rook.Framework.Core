
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Rook.Framework.Core.HttpServerAspNet.ModelBinding.Providers;
using static Rook.Framework.Core.HttpServerAspNet.ModelBinding.Source;

#if NET451
using Microsoft.AspNetCore.Mvc.Internal;
#else
using Microsoft.AspNetCore.Mvc.Infrastructure;
#endif

namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    public class DefaultPassthroughHybridModelBinder: HybridModelBinder
    {
        public DefaultPassthroughHybridModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory)
            : base(Strategy.Passthrough)
        {
            base
                .AddModelBinder(Body, new BodyModelBinder(formatters, readerFactory))
                .AddValueProviderFactory(Form, new FormValueProviderFactory())
                .AddValueProviderFactory(Source.Route, new RouteValueProviderFactory())
                .AddValueProviderFactory(Source.QueryString, new QueryStringValueProviderFactory())
                .AddValueProviderFactory(Header, new HeaderValueProviderFactory());
        }
    }
}