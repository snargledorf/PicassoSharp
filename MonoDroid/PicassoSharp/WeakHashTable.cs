using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PicassoSharp
{
    class WeakHashtable<TKey, TValue>
        where TKey : class 
        where TValue : class
    {
        private readonly Dictionary<int, WeakReference<TKey>> m_Keys = new Dictionary<int, WeakReference<TKey>>();
        private readonly ConditionalWeakTable<TKey, TValue> m_InternalTable = new ConditionalWeakTable<TKey, TValue>();
 
        public void Clear()
        {
            foreach (WeakReference<TKey> key in m_Keys.Values)
            {
                TKey keyValue;
                if (!key.TryGetTarget(out keyValue))
                    continue;

                m_InternalTable.Remove(keyValue);
            }

            m_Keys.Clear();
        }

        public int Count
        {
            get { return m_Keys.Count; }
        }

        public void Add(TKey key, TValue value)
        {
            Remove(key);
            m_InternalTable.Add(key, value);
            m_Keys[key.GetHashCode()] = new WeakReference<TKey>(key);
        }

        public bool ContainsKey(TKey key)
        {
            return m_Keys.ContainsKey(key.GetHashCode());
        }

        public bool Remove(TKey key)
        {
            return m_InternalTable.Remove(key) && m_Keys.Remove(key.GetHashCode());
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_InternalTable.TryGetValue(key, out value);
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (WeakReference<TKey> key in m_Keys.Values)
                {
                    TKey keyValue;
                    if (!key.TryGetTarget(out keyValue))
                        continue;

                    TValue value;
                    if (!m_InternalTable.TryGetValue(keyValue, out value))
                        continue;

                    yield return value;
                }
            }
        }
    }
}