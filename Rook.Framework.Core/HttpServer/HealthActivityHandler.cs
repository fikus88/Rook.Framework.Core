using System.Collections.Generic;
using System.Linq;
using System.Net;
using Rook.Framework.Core.Health;

namespace Rook.Framework.Core.HttpServer
{
    public class HealthActivityHandler : IActivityHandler
    {
        private readonly IEnumerable<IHealthCheck> _healthChecks;
        public dynamic ExampleRequestDocument => new { };

        public dynamic ExampleResponseDocument => new { };

        public HealthActivityHandler(IEnumerable<IHealthCheck> healthChecks)
        {
            _healthChecks = healthChecks;
        }

        public void Handle(IHttpRequest request, IHttpResponse response)
        {
            var isHealthy = _healthChecks.All(x => x.IsHealthy());
            
            if (isHealthy)
            {
                response.SetStringContent("All clear");
                response.HttpStatusCode = HttpStatusCode.OK;
            }
            else
            {
                response.SetStringContent("Broken");
                response.HttpStatusCode = HttpStatusCode.InternalServerError;
            }
        }
    }
}