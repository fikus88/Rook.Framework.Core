using System.Linq;

namespace Rook.Framework.Core.Application.ResponseHandlers
{
	public static class BusResponseHelper
	{
		public static bool HasNoErrorsAndSolutionIsNotEmpty<TSolution>(this BusResponse<TSolution> response)
		{
			return string.IsNullOrEmpty(response.Errors) && response.Solution != null && response.Solution.Any();
		}
	}
}