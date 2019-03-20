using System.Net;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.HttpServer;
using StructureMap;

namespace Rook.Framework.Core.StructureMap.Registries
{
    public class HttpRegistry : Registry
    {
        public HttpRegistry()
        {
            For<IHttpResponse>().AlwaysUnique().Use<HttpResponse>();

            For<IHttpResponse>().AlwaysUnique().Add(x => new HttpResponse(x.GetInstance<IDateTimeProvider>(), x.GetInstance<ILogger>())
            {
                HttpStatusCode = HttpStatusCode.Unauthorized
            }).Named("Unauthorised");

            For<IHttpResponse>().AlwaysUnique().Add(x => new HttpResponse(x.GetInstance<IDateTimeProvider>(), x.GetInstance<ILogger>())
            {
                HttpStatusCode = HttpStatusCode.NotFound,
                HttpContent = new EmptyHttpContent()
            }).Named("NotFound");
        }
    }
}