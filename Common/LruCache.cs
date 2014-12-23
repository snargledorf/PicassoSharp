using System;
using System.Collections.Generic;

namespace PicassoSharp
{
    public class LruCache<T> : IDisposable, ICache<T> 
        where T : IDisposable
	{
		private readonly object m_Sync = new object();

        private readonly Dictionary<String, T> m_Dictionary;
        private readonly LinkedList<String> m_List;
        private readonly Func<T, int> m_SizeOfFunc;
        private readonly int m_SizeLimit;
        private int m_CurrentSize;
        private bool m_Disposed;

        public LruCache(int sizeLimit, Func<T, int> sizeOfOf) 
        {
            if (sizeLimit != 0 && sizeOfOf == null)
                throw new ArgumentNullException("sizeLimit");

            m_List = new LinkedList<String>();
            m_Dictionary = new Dictionary<String, T>();

            m_SizeLimit = sizeLimit;
            m_SizeOfFunc = sizeOfOf;
        }

        protected virtual void OnEvict(T value)
        {
			value.Dispose ();
        }

        void Evict()
        {
            var key = m_List.Last.Value;
            var last = m_Dictionary[key];

            if (m_SizeLimit > 0)
            {
                int size = m_SizeOfFunc(last);
                m_CurrentSize -= size;
            }

            m_Dictionary.Remove(key);
            m_List.RemoveLast();
            OnEvict(last);

            System.Diagnostics.Debug.WriteLine("Evicted, got: {0} bytes and {1} slots", m_CurrentSize, m_List.Count);
        }

        public void Purge()
        {
			lock (m_Sync)
			{
				foreach (var value in m_Dictionary.Values)
				{
					OnEvict(value);
				}

				m_Dictionary.Clear();
				m_List.Clear();
				m_CurrentSize = 0;
			}
        }

        public T this[String key]
        {
            get
            {
                return Get(key);
            }

            set
            {
                Set(key, value);
            }
        }

        public T Get(String key) 
        {
			lock (m_Sync)
			{
                T value;
				if (m_Dictionary.TryGetValue(key, out value))
				{
					m_List.Remove(key);
					m_List.AddFirst(key);
					return value;
				}
                return default(T);
			}
        }

        public void Set(String key, T value) 
        {
			lock (m_Sync)
			{
                int valueSize = m_SizeOfFunc(value);

                if (valueSize > m_SizeLimit)
                    throw new ArgumentException(String.Format("Value larger than cache: Entry Size={0} Cache Size={1}", valueSize, m_SizeLimit));

                T currentValue;
				if (m_Dictionary.TryGetValue(key, out currentValue))
				{
					// Is this a new value?
					if (!currentValue.Equals(value))
					{
						int nodeSize = m_SizeOfFunc(currentValue);
						m_CurrentSize -= nodeSize;
						m_CurrentSize += valueSize;

						// Remove the old value
						currentValue.Dispose();
						m_Dictionary[key] = value;
					}

					// Set the key to the front
					m_List.Remove(key);
					m_List.AddFirst(key);
				}
				else
				{
					// Check if we will be above the size limit before adding a new value
					while (m_CurrentSize + valueSize > m_SizeLimit && m_List.Count > 0)
						Evict();

					// Adding the new value
					m_List.AddFirst(key);
					m_Dictionary[key] = value;
					m_CurrentSize += valueSize;
                    System.Diagnostics.Debug.WriteLine("new size: {0} with {1}", m_CurrentSize, m_List.Count);
				}

				while (m_CurrentSize > m_SizeLimit && m_List.Count > 1)
					Evict();
			}
        }

        public void Clear()
        {
			lock (m_Sync)
			{
				m_List.Clear();
				m_Dictionary.Clear();
			}
        }

        #region IDisposable implementation
        ~LruCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                foreach (var item in m_Dictionary.Values)
                {
                    OnEvict(item);
                }
            }

            m_Disposed = true;
        }
        #endregion

        public override string ToString()
        {
            return "LRUCache dict={0} revdict={1} list={2}";
        }
    }
}

