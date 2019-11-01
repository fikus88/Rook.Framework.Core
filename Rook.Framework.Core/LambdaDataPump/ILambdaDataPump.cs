using System.Threading.Tasks;
using Amazon.Lambda.Model;

namespace Rook.Framework.Core.LambdaDataPump
{
    public interface ILambdaDataPump
    {
        Task<InvokeResponse> InvokeLambdaAsync(string payload);
    }
}