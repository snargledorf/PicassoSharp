using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Java.Util;
using Java.Util.Concurrent;

namespace PicassoSharp
{
	public sealed class Picasso : IDisposable
	{
		public const int BatchComplete = 1;

	    private static readonly Handler Handler = new PicassoSharpHandler();
        
        private static Picasso s_Instance;

	    public static Picasso With(Context context)
	    {
	        return s_Instance ?? (s_Instance = new Builder(context).Build());
	    }

		private readonly Context m_Context;
        private IExecutorService m_Executor;
	    private readonly IDownloader m_Downloader;
	    private readonly ICache m_Cache;
        private readonly Dispatcher m_Dispatcher;
        private readonly ConditionalWeakTable<Object, Action> m_TargetToAction;

	    private bool m_Disposed;

	    public Context Context
		{
			get
			{
				return m_Context;
			}
		}

		private Picasso(Context context, ICache cache, IExecutorService executor, IDownloader downloader, Dispatcher dispatcher)
        {
			m_Context = context;
			m_Cache = cache;
			m_Executor = executor;
		    m_Downloader = downloader;
		    m_Dispatcher = dispatcher;
		    m_TargetToAction = new ConditionalWeakTable<Object, Action>();
		}

		public ICache Cache
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

			m_Executor.Shutdown();

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
			var cachedImage = m_Cache.Get(action.Key);
			if (cachedImage != null && !cachedImage.IsRecycled)
			{
				CompleteAction(cachedImage, action, LoadedFrom.Memory);
			}
			else
			{
				var target = action.Target;
				if (target != null)
				{
					CancelExistingRequest(target);
					LinkTargetToAction(target, action);
				}
				m_Dispatcher.DispatchSubmit(action);
			}
		}

		public void CancelRequest(ImageView target)
		{
			CancelExistingRequest(target);
		}

        public void CancelRequest(ITarget target)
        {
            CancelExistingRequest(target);
        }

		private void Complete(BitmapHunter hunter)
		{
			Action action = hunter.Action;
			List<Action> actions = hunter.Actions;
			Bitmap result = hunter.Result;
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

		private void CompleteAction(Bitmap result, Action action, LoadedFrom loadedFrom)
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
            private readonly Context m_Context;
            private ICache m_Cache;
            private IExecutorService m_Service;
            private IDownloader m_Downloader;

            public Builder(Context context)
            {
                m_Context = context.ApplicationContext;
            }

            public Builder Cache(ICache cache)
            {
                m_Cache = cache;
                return this;
            }

            public Builder Executor(IExecutorService executor)
            {
                m_Service = executor;
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
                    int cacheSize = AndroidUtils.CalculateCacheSize(m_Context);
                    m_Cache = new LruCache(cacheSize);
                }

                if (m_Service == null)
                {
                    m_Service = new PicassoExecutorService();
                }

                if (m_Downloader == null)
                {
                    m_Downloader = new WebClientDownloader();
                }

                Dispatcher dispatcher = new Dispatcher(m_Context, Handler, m_Service, m_Cache, m_Downloader);

                return new Picasso(m_Context, m_Cache, m_Service, m_Downloader, dispatcher);
            }
        }

        private class PicassoSharpHandler : Handler
        {
            public PicassoSharpHandler() 
                : base(Looper.MainLooper)
            {
                
            }

            public override void HandleMessage(Message msg)
            {
                switch (msg.What)
                {
                    case BatchComplete:
                        ArrayList hunters = (ArrayList)msg.Obj;
                        for (int i = 0; i < hunters.Size(); i++)
                        {
                            BitmapHunter hunter = (BitmapHunter) hunters.Get(i);
                            hunter.Picasso.Complete(hunter);
                        }
                        break;
                    // TODO Request Garbage Collection
                }
            }
        }
    }
}
