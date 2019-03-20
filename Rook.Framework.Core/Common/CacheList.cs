using System;
using System.Collections.Generic;
using System.Linq;

namespace Rook.Framework.Core.Common
{
    public abstract class CacheListBase<TKey, TValue>
    {
        protected Func<TKey, TValue> Getter;

        public abstract TValue this[TKey key] { get; set; }

        /// <summary>
        /// Updates the Getter method for undiscovered or not-yet-loaded items.
        /// </summary>
        /// <param name="func"></param>
        public void SetGetterMethod(Func<TKey, TValue> func)
        {
            Getter = func;
        }
    }

    public sealed class CacheList<TKey1, TKey2, TValue> : CacheList<Tuple<TKey1, TKey2>, TValue>
    where TKey1 : struct
    where TKey2 : struct
    {
    }

    public sealed class CacheList<TKey1, TKey2, TKey3, TValue> : CacheList<Tuple<TKey1, TKey2, TKey3>, TValue>
        where TKey1 : struct
        where TKey2 : struct
        where TKey3 : struct
    {
    }

    public class CacheList<TKey, TValue> : CacheListBase<TKey, TValue>
    {
        private readonly bool enableTouch;
        private readonly List<CacheRecord> internalList = new List<CacheRecord>();

        private readonly int maxCacheSize = int.MaxValue;

        private readonly TimeSpan timeout = TimeSpan.MaxValue;

        public CacheList() { }

        public CacheList(Func<TKey, TValue> getter)
        {
            Getter = getter;
        }

        public CacheList(Func<TKey, TValue> getter, TimeSpan timeout)
            : this(getter)
        {
            this.timeout = timeout;
        }

        public CacheList(Func<TKey, TValue> getter, TimeSpan timeout, bool enableTouch)
            : this(getter, timeout)
        {
            this.enableTouch = enableTouch;
        }

        public CacheList(Func<TKey, TValue> getter, TimeSpan timeout, bool enableTouch, int maxCacheSize)
            : this(getter, timeout, enableTouch)
        {
            this.maxCacheSize = maxCacheSize;
        }

        public override TValue this[TKey key]
        {
            get
            {
                lock (this)
                {
                    CacheRecord cacheRecord = GetCacheRecord(key);
                    return cacheRecord.Value;
                }
            }
            set
            {
                lock (this)
                {
                    if (!ContainsKey(key))
                    {
                        if (internalList.Count == maxCacheSize)
                            internalList.Remove(internalList.OrderBy(cr => cr.LastTouched).First());
                        CacheRecord cacheRecord = new CacheRecord(key, Getter, timeout, enableTouch);
                        cacheRecord.Value = value;
                        internalList.Add(cacheRecord);
                    }
                    else
                    {
                        CacheRecord cacheRecord = GetCacheRecord(key);
                        cacheRecord.Value = value;
                    }
                }
            }
        }

        private CacheRecord GetCacheRecord(TKey key)
        {
            CacheRecord cacheRecord;
            if (!ContainsKey(key))
            {
                if (internalList.Count == maxCacheSize)
                    internalList.Remove(internalList.OrderBy(cr => cr.LastTouched).First());
                cacheRecord = new CacheRecord(key, Getter, timeout, enableTouch);
                internalList.Add(cacheRecord);
            }
            else
            {
                cacheRecord = internalList.First(cr => cr.Key.Equals(key));
            }
            return cacheRecord;
        }

        protected bool ContainsKey(TKey key)
        {
            return internalList.Any(cr => cr.Key.Equals(key));
        }

        public void Clear()
        {
            internalList.Clear();
        }

        private sealed class CacheRecord
        {
            private readonly bool enableTouch;
            private readonly Func<TKey, TValue> getterFunction;

            private readonly TKey key;

            private readonly TimeSpan timeout;
            private bool hasValue;
            private DateTime lastTouched = DateTime.Now;
            private TValue value;

            public CacheRecord(TKey key, Func<TKey, TValue> getterFunction, TimeSpan timeout, bool enableTouch)
            {
                this.key = key;
                this.getterFunction = getterFunction;
                this.timeout = timeout;
                this.enableTouch = enableTouch;
            }

            public DateTime LastTouched => lastTouched;

            public TKey Key => key;

            public TValue Value
            {
                get
                {
                    if ((!hasValue || DateTime.Now - LastTouched > timeout))
                    {
                        value = getterFunction(Key);
                        hasValue = true;
                        lastTouched = DateTime.Now;
                    }
                    if (enableTouch) lastTouched = DateTime.Now;
                    return value;
                }
                set
                {
                    this.value = value;
                    hasValue = true;
                    if (enableTouch) lastTouched = DateTime.Now;
                }
            }
        }
    }
}