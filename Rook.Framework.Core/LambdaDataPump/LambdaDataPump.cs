using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using KafkaNet;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.LambdaDataPump
{
    public class LambdaDataPump : ILambdaDataPump
    {
        private readonly ILogger _logger;

        private readonly IAmazonLambda _amazonLambda;

        private readonly string _lambdaName;

        public LambdaDataPump(ILogger logger, string functionName)
        {
            _logger = logger;
            _amazonLambda = new AmazonLambdaClient();
            _lambdaName = functionName;
        }

        public async Task<InvokeResponse> InvokeLambdaAsync(string payload)
        {
            var request = new InvokeRequest()
            {
                FunctionName = _lambdaName,
                Payload = payload,
                InvocationType = InvocationType.Event,
                Qualifier = "$LATEST",
            };

            return await _amazonLambda.InvokeAsync(request);
        }
    }
}