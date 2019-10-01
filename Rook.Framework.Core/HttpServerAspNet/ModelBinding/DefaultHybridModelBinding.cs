
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Rook.Framework.Core.HttpServerAspNet.ModelBinding.Providers;
using static Rook.Framework.Core.HttpServerAspNet.ModelBinding.Source;


namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    public class DefaultHybridModelBinder  : HybridModelBinder
    {
        public DefaultHybridModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory)
            : base(Strategy.FirstInWins)
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