using System;
using System.Collections;
using System.Collections.Generic;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.Utils
{
    public class CaseInsensitiveDictionary:IDictionary<string,string>
    {
        private readonly AutoDictionary<string,string> inner = new AutoDictionary<string, string>();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();

        public void Add(KeyValuePair<string, string> item) => Add(item.Key,item.Value);

        public void Clear() => inner.Clear();

        public bool Contains(KeyValuePair<string, string> item) => inner.ContainsKey(item.Key.ToLower());
        
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, string> item) => inner.Remove(item.Key.ToLower());

        public int Count => inner.Count;
        public bool IsReadOnly => false;
        public void Add(string key, string value) => inner.Add(key.ToLower(), value);

        public bool ContainsKey(string key) => inner.ContainsKey(key.ToLower());

        public bool Remove(string key) => inner.Remove(key.ToLower());

        public bool TryGetValue(string key, out string value) => inner.TryGetValue(key.ToLower(), out value);
        
        public string this[string key]
        {
            get => inner[key.ToLower()];
            set => inner[key.ToLower()] = value;
        }

        public ICollection<string> Keys => inner.Keys;
        public ICollection<string> Values => inner.Values;
    }
}
