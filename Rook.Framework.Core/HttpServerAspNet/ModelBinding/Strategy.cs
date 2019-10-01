using System.Collections.Generic;
using System.Linq;


namespace Rook.Framework.Core.HttpServerAspNet.ModelBinding
{
    public static class Strategy
    {
        public static bool FirstInWins(
            IEnumerable<string> previouslyBoundValueProviderIds,
            IEnumerable<string> allValueProviderIds)
        {
            return !previouslyBoundValueProviderIds.Any();
        }

        public static bool Passthrough(
            IEnumerable<string> previouslyBoundValueProviderIds,
            IEnumerable<string> allValueProviderIds)
        {
            return true;
        }
    }
}