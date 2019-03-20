using System.Diagnostics;
using System.Net;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.HttpServer
{
    /// <summary>
    /// Internal implementation of IRequestBroker used for only internal
    /// HTTP functionality, such as the metrics endpoint.
    /// 
    /// Does not offer full API functionality - use Rook.MicroService.Core.Api
    /// for that.
    /// </summary>
    public sealed class RequestBroker : IRequestBroker
    {
        private readonly ILogger _logger;
        private readonly IContainerFacade _container;

        public RequestBroker(ILogger logger, IContainerFacade container)
        {
            _logger = logger;
            _container = container;
        }

        public IHttpResponse HandleRequest(IHttpRequest request, TokenState tokenState)
        {
            _logger.Trace($"{nameof(RequestBroker)}.{nameof(HandleRequest)}", new LogItem("Event", "GetRequestHandler started"));

            Stopwatch timer = Stopwatch.StartNew();
            IActivityHandler handler = GetRequestHandler(request);

            var handlerName = handler != null ? handler.GetType().Name : "null";

            _logger.Trace($"{nameof(RequestBroker)}.{nameof(HandleRequest)}", new LogItem("Event", "GetRequestHandler completed"), 
                new LogItem("DurationMilliseconds", timer.Elapsed.TotalMilliseconds), 
                new LogItem("FoundHandler", handlerName));

            if (handler == null)
                return null;

            IHttpResponse response = _container.GetInstance<IHttpResponse>(true);

            _logger.Trace($"{nameof(RequestBroker)}.{nameof(HandleRequest)}", new LogItem("Event", "Handler Handle called"));
            timer.Restart();

            handler.Handle(request, response);
            _logger.Trace($"{nameof(RequestBroker)}.{nameof(HandleRequest)}", new LogItem("Event", "Handler Handle completed"), new LogItem("DurationMilliseconds", timer.Elapsed.TotalMilliseconds));

            return response;            
        }

        public int Precedence { get; } = 0;

        private IActivityHandler GetRequestHandler(IHttpRequest request)
        {
            if (request.Verb == HttpVerb.Get && request.Path == "/metrics")
                return _container.GetInstance<MetricsActivityHandler>();

            if (request.Verb == HttpVerb.Get && request.Path == "/health")
                return _container.GetInstance<HealthActivityHandler>();

            return null;
        }
    }
}
