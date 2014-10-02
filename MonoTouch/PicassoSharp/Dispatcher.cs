using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	internal class Dispatcher
    {
        private const int BatchDelay = 200;

	    private readonly ICache<UIImage> m_Cache;
	    private readonly IDownloader m_Downloader;

        private readonly Dictionary<String, BitmapHunter> m_Hunters = new Dictionary<String, BitmapHunter>();
        private readonly List<BitmapHunter> m_Batch = new List<BitmapHunter>();

        private readonly Object m_Lock = new Object();

	    private bool m_AirplaneMode;

	    internal Dispatcher(ICache<UIImage> cache, IDownloader downloader)
	    {
	        m_Cache = cache;
	        m_Downloader = downloader;
            m_AirplaneMode = IOSUtils.IsAirplaneModeOn();
        }

	    internal async void DispatchSubmit(Action action)
	    {
	        await Task.Factory.StartNew(() =>
	        {
	            BitmapHunter hunter;
	            lock (m_Lock)
	            {
	                if (m_Hunters.TryGetValue(action.Key, out hunter))
	                {
	                    hunter.Attach(action);
	                    return;
	                }

		            hunter = BitmapHunter.ForRequest(action.Picasso, action, this, m_Cache, m_Downloader);
		            hunter.Run();
	                m_Hunters.Add(action.Key, hunter);
				}
	        });
	    }

        internal async void DispatchCancel(Action action)
        {
            await Task.Factory.StartNew(() =>
            {
                string key = action.Key;
                BitmapHunter hunter;
                lock (m_Lock)
                {
                    m_Hunters.TryGetValue(key, out hunter);
                }

                if (hunter == null) return;

                hunter.Detach(action);
                hunter.Cancel();
                if (!hunter.IsCancelled) return;

                lock (m_Lock)
                {
                    m_Hunters.Remove(key);
                }
            });
        }

        internal async void DispatchComplete(BitmapHunter hunter)
        {
            await Task.Factory.StartNew(() =>
            {
                lock (m_Lock)
                {
                    m_Hunters.Remove(hunter.Key);
                }
                Batch(hunter);
            });
        }

        internal async void DispatchFailed(BitmapHunter hunter)
        {
            await Task.Factory.StartNew(() =>
            {
                lock (m_Lock)
                {
                    m_Hunters.Remove(hunter.Key);
                }
                Batch(hunter);
            });
        }

	    private async void DispatchAirplaneModeChange(bool airplaneMode)
	    {
	        await Task.Factory.StartNew(() =>
	        {
	            lock (m_Lock)
	            {
	                m_AirplaneMode = airplaneMode;
	            }
	        });
	    }

	    private void PerformBatchComplete()
	    {
	        BitmapHunter[] copy;
	        lock (m_Lock)
	        {
                copy = m_Batch.ToArray();
	            m_Batch.Clear();
	        }
	        Picasso.BatchComplete(copy);
	    }

        private void Batch(BitmapHunter hunter)
        {
            if (hunter.IsCancelled)
            {
                return;
            }

            lock (m_Lock)
            {
                m_Batch.Add(hunter);
            }

            Task.Delay(BatchDelay).ContinueWith((t) => PerformBatchComplete());
        }
    }
}

