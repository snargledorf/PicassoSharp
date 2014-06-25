using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public sealed class Picasso : IDisposable
	{
        private static Picasso s_Instance;

	    public static Picasso DefaultInstance
	    {
	        get
            {
                return s_Instance ?? (s_Instance = new Builder().Build());
	        }
	    }

        private static readonly NSObject s_MainThreadInvokeObject = new NSObject();

        private readonly ICache<UIImage> m_Cache;
        private readonly Dispatcher m_Dispatcher;
        private readonly ConditionalWeakTable<Object, Action> m_TargetToAction;

	    private bool m_Disposed;

        private Picasso(ICache<UIImage> cache, Dispatcher dispatcher)
        {
			m_Cache = cache;
		    m_Dispatcher = dispatcher;
		    m_TargetToAction = new ConditionalWeakTable<Object, Action>();
		}

        public ICache<UIImage> Cache
		{
			get
			{
				return m_Cache;
			}
		}

	    public RequestCreator Load(string path)
	    {
	        return Load(new Uri(path));
	    }

	    public RequestCreator Load(Uri uri)
	    {
	        return new RequestCreator(this, uri);
	    }

        public bool IsShutdown { get; private set; }

        public void Shutdown()
        {
            if (this == s_Instance)
                throw new NotSupportedException("Default instance cannot be shutdown.");

            if (IsShutdown)
				return;

			m_Cache.Clear();

            IsShutdown = true;
		}

		private void CancelExistingRequest(Object target)
		{
			Action action;
			if (m_TargetToAction.TryGetValue(target, out action))
			{
				action.Cancel();
				m_Dispatcher.DispatchCancel(action);
			}
			m_TargetToAction.Remove(target);
		}

		private void LinkTargetToAction(Object target, Action action)
		{
			m_TargetToAction.Add(target, action);
		}

		internal void EnqueueAndSubmit(Action action)
		{
			UIImage cachedImage = m_Cache.Get(action.Key);
			if (cachedImage != null)
			{
				CompleteAction(cachedImage, action, LoadedFrom.Memory);
			}
			else
			{
				object target = action.Target;
				if (target != null)
				{
					CancelExistingRequest(target);
					LinkTargetToAction(target, action);
				}
				m_Dispatcher.DispatchSubmit(action);
			}
		}

		public void CancelRequest(UIImageView target)
		{
			CancelExistingRequest(target);
		}

        public void CancelRequest(ITarget target)
        {
            CancelExistingRequest(target);
        }

        internal static void BatchComplete(BitmapHunter[] hunters)
        {
            s_MainThreadInvokeObject.InvokeOnMainThread(() =>
            {
                foreach (BitmapHunter hunter in hunters)
                {
                    hunter.Picasso.Complete(hunter);
                }
            });
        }

		private void Complete(BitmapHunter hunter)
		{
			Action action = hunter.Action;
			List<Action> actions = hunter.Actions;
			UIImage result = hunter.Result;
			LoadedFrom loadedFrom = hunter.LoadedFrom;

			if (action != null)
			{
				CompleteAction(result, action, loadedFrom);
			}

			if (actions != null)
			{
				foreach (Action action_ in actions)
				{
					CompleteAction(result, action_, loadedFrom);
				}
			}
		}

		private void CompleteAction(UIImage result, Action action, LoadedFrom loadedFrom)
		{
            if (action.Cancelled)
                return;

			m_TargetToAction.Remove(action.Target);

			if (result != null)
			{
				action.Complete(result, loadedFrom);
			}
			else
			{
				action.Error();
			}
		}

	    ~Picasso()
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
	            Shutdown();
	        }

	        m_Disposed = true;
	    }

        public class Builder
        {
            private ICache<UIImage> m_Cache;
            private IDownloader m_Downloader;

            public Builder()
            {
            }

            public Builder Cache(ICache<UIImage> cache)
            {
                m_Cache = cache;
                return this;
            }

            public Builder Downloader(IDownloader downloader)
            {
                m_Downloader = downloader;
                return this;
            }

            public Picasso Build()
            {
                if (m_Cache == null)
                {
                    int cacheSize = IOSUtils.CalculateCacheSize();
                    m_Cache = new LruCache<UIImage>(cacheSize);
                }

                if (m_Downloader == null)
                {
                    m_Downloader = new WebClientDownloader();
                }

                Dispatcher dispatcher = new Dispatcher(m_Cache, m_Downloader);

                return new Picasso(m_Cache, dispatcher);
            }
        }
    }
}
