using System.Collections.Generic;

namespace Rook.Framework.Core.Common
{
    public interface IConfigurationManager
    {
        IDictionary<string, string> AppSettings { get; }
        IDictionary<string, string> Environment { get; }
        T Get<T>(string key);
        T Get<T>(string key, T fallback);
    }
}
