using Rook.Framework.Core.StructureMap.Registries;
using StructureMap;

namespace Rook.Framework.Core.StructureMap
{
    public static class Bootstrapper
    {
        public static IContainer Init()
        {
            return Container.For<MicroserviceRegistry>();
        }
    }
}
