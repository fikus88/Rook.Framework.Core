using Newtonsoft.Json;

namespace Rook.Framework.Core.Common
{
    internal sealed class Serialise<T>
    {
        private readonly T _value;

        internal Serialise(T value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(_value, Formatting.Indented);
        }
    }
}