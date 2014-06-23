using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Graphics;

namespace PicassoSharp
{
    public class LruCache : IDisposable, ICache
	{
		private readonly object m_Sync = new object();

        private Dictionary<String, Bitmap> m_Dictionary;
        private LinkedList<String> m_List;
        private Func<Bitmap, int> m_SlotSizeFunc;
        private int m_SizeLimit;
        private int m_EntryLimit;
        private int m_CurrentSize;
        private bool m_Disposed;

        public LruCache(int entryLimit) 
            : this(entryLimit, 0, null)
        {
        }

        public LruCache(int sizeLimit, Func<Bitmap, int> slotSizer) 
            : this(0, sizeLimit, slotSizer)
        {
        }

        public LruCache(int entryLimit, int sizeLimit, Func<Bitmap, int> slotSizer)
        {
            if (sizeLimit != 0 && slotSizer == null)
                throw new ArgumentNullException("If sizeLimit is set, the slotSizer must be provided");

            m_List = new LinkedList<String>();
            m_Dictionary = new Dictionary<String, Bitmap>();

            m_EntryLimit = entryLimit;
            m_SizeLimit = sizeLimit;
            m_SlotSizeFunc = slotSizer;
        }

        protected virtual void OnEvict(Bitmap value)
        {
			value.Dispose ();
        }

        void Evict()
        {
            var key = m_List.Last.Value;
            var last = m_Dictionary[key];

            if (m_SizeLimit > 0)
            {
                int size = m_SlotSizeFunc(last);
                m_CurrentSize -= size;
            }

            m_Dictionary.Remove(key);
            m_List.RemoveLast();
            OnEvict(last);

            Console.WriteLine("Evicted, got: {0} bytes and {1} slots", m_CurrentSize, m_List.Count);
        }

        public void Purge()
        {
			lock (m_Sync)
			{
				foreach (var element in m_Dictionary.Values)
				{
					OnEvict(element);
				}

				m_Dictionary.Clear();
				m_List.Clear();
				m_CurrentSize = 0;
			}
        }

        public Bitmap this[String key]
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

        public Bitmap Get(String key) 
        {
			lock (m_Sync)
			{
                Bitmap node;
				if (m_Dictionary.TryGetValue(key, out node))
				{
					m_List.Remove(key);
					m_List.AddFirst(key);
					return node;
				}
                return null;
			}
        }

        public void Set(String key, Bitmap newValue) 
        {
			lock (m_Sync)
			{
				int valueSize = m_SizeLimit > 0 ? m_SlotSizeFunc(newValue) : 0;

                Bitmap currentValue;
				if (m_Dictionary.TryGetValue(key, out currentValue))
				{
					// Is this a new value?
					if (!currentValue.Equals(newValue))
					{
						if (m_SizeLimit > 0)
						{
							int nodeSize = m_SlotSizeFunc(currentValue);
							m_CurrentSize -= nodeSize;
							m_CurrentSize += valueSize;
						}

						// Remove the old value
						currentValue.Dispose();
						m_Dictionary[key] = newValue;
					}

					// If we already have a key, move it to the front
					m_List.Remove(key);
					m_List.AddFirst(key);
				}
				else
				{
					// Check we will be above the size limit before adding a new entry
					while (m_SizeLimit > 0 && m_CurrentSize + valueSize > m_SizeLimit && m_List.Count > 0)
						Evict();

					if (m_EntryLimit > 0 && m_List.Count >= m_EntryLimit)
						Evict();

					// Adding the new entry
					m_List.AddFirst(key);
					m_Dictionary[key] = newValue;
					m_CurrentSize += valueSize;
					Console.WriteLine("new size: {0} with {1}", m_CurrentSize, m_List.Count);
				}

				while (m_SizeLimit > 0 && m_CurrentSize > m_SizeLimit && m_List.Count > 1)
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

