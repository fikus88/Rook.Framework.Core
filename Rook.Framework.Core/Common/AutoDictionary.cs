using System.Collections;
using System.Collections.Generic;

namespace Rook.Framework.Core.Common
{
    /// <summary>Like a Dictionary&lt;TKey, TValue&gt;, but returns default(TValue) when the key is not present, and allows setting of the value by key even if it doesn't yet exist.</summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class AutoDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> innerDictionary = new Dictionary<TKey, TValue>();

        public AutoDictionary(IDictionary dictionary)
        {
            foreach (object k in dictionary.Keys)
                this[(TKey)k] = (TValue)dictionary[k];
        }

        public AutoDictionary() { }

        public void Add(TKey key, TValue value) => innerDictionary.Add(key, value);

        public bool ContainsKey(TKey key) => key!=null && innerDictionary.ContainsKey(key);

        public bool Remove(TKey key) => innerDictionary.Remove(key);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!innerDictionary.TryGetValue(key, out value))
                value = default(TValue);
            return true;
        }

        public TValue this[TKey key]
        {
            get => ContainsKey(key) ? innerDictionary[key] : default(TValue);
            set
            {
                if (ContainsKey(key)) innerDictionary[key] = value;
                else Add(key, value);
            }
        }

        public ICollection<TKey> Keys => innerDictionary.Keys;
        public ICollection<TValue> Values => innerDictionary.Values;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => innerDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => innerDictionary.GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => innerDictionary.Add(item.Key, item.Value);

        public void Clear() { innerDictionary.Clear(); }

        public bool Contains(KeyValuePair<TKey, TValue> item) => innerDictionary.ContainsKey(item.Key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, TValue> pair in innerDictionary)
                array[arrayIndex++] = pair;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => false;

        public int Count => innerDictionary.Count;
        public bool IsReadOnly => false;
    }
}
