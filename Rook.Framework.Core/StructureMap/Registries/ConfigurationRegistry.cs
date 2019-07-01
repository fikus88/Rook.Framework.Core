using Rook.Framework.Core.Common;
using StructureMap;

namespace Rook.Framework.Core.StructureMap.Registries
{
	public class ConfigurationRegistry : Registry
	{
		public ConfigurationRegistry()
		{
			For<IConfigurationManager>().Use<ConfigurationManager>().Singleton();
		}
	}
}
