using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public sealed class Picasso : IPicasso<UIImage, UIImage>
	{
        private static Picasso s_Instance;

	    public static Picasso DefaultInstance
	    {
	        get
            {
                return s_Instance ?? (s_Instance = new Builder().Build());
	        }
	    }

        private static readonly NSObject MainThreadInvokeObject = new NSObject();

        private readonly ICache<UIImage> m_Cache;
        private readonly Dispatcher m_Dispatcher;
        private readonly IList<IRequestHandler<UIImage>> m_RequestHandlers;
        private readonly ConditionalWeakTable<Object, Action<UIImage, UIImage>> m_TargetToAction;

	    private bool m_Disposed;

        private Picasso(ICache<UIImage> cache, IRequestTransformer<UIImage> requestTransformer, List<RequestHandler> extraRequestHandlers, Dispatcher dispatcher)
        {
			m_Cache = cache;
		    m_Dispatcher = dispatcher;
            m_TargetToAction = new ConditionalWeakTable<Object, Action<UIImage, UIImage>>();

            const int builtInHandlersCount = 3;
            int extraHandlersCount = extraRequestHandlers != null ? extraRequestHandlers.Count : 0;
            var allRequestHandlers = new List<IRequestHandler<UIImage>>(builtInHandlersCount + extraHandlersCount);

            if (extraRequestHandlers != null)
            {
                allRequestHandlers.AddRange(allRequestHandlers);
            }
            allRequestHandlers.Add(new FileRequestHandler());
            allRequestHandlers.Add(new NetworkRequestHandler(dispatcher.Downloader));
            m_RequestHandlers = new ReadOnlyCollection<IRequestHandler<UIImage>>(allRequestHandlers);
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
        
        public IList<IRequestHandler<UIImage>> RequestHandlers
        {
            get { return m_RequestHandlers; }
        }

	    void IPicasso<UIImage, UIImage>.CancelExistingRequest(object target)
	    {
	        CancelExistingRequest(target);
	    }

        void IPicasso<UIImage, UIImage>.Complete(IBitmapHunter<UIImage, UIImage> hunter)
        {
            Action<UIImage, UIImage> action = hunter.Action;
            List<Action<UIImage, UIImage>> actions = hunter.Actions;
            UIImage result = hunter.Result;
            LoadedFrom loadedFrom = hunter.LoadedFrom;

            if (action != null)
            {
                CompleteAction(result, action, loadedFrom);
            }

            if (actions != null)
            {
                foreach (Action<UIImage, UIImage> action_ in actions)
                {
                    CompleteAction(result, action_, loadedFrom);
                }
            }
	    }

        public void RunOnPicassoThread(Action action)
	    {
            MainThreadInvokeObject.InvokeOnMainThread(() => action());
	    }

	    private void CancelExistingRequest(Object target)
		{
            Action<UIImage, UIImage> action;
			if (m_TargetToAction.TryGetValue(target, out action))
			{
				action.Cancel();
				m_Dispatcher.DispatchCancel(action);
			}
			m_TargetToAction.Remove(target);
		}

		private void LinkTargetToAction(Object target, Action<UIImage, UIImage> action)
		{
			m_TargetToAction.Add(target, action);
		}

		internal void EnqueueAndSubmit(Action<UIImage, UIImage> action)
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

        public void CancelRequest(ITarget<UIImage,UIImage,UIImage> target)
        {
            CancelExistingRequest(target);
        }

        internal static void BatchComplete(BitmapHunter[] hunters)
        {
            MainThreadInvokeObject.InvokeOnMainThread(() =>
            {
                foreach (BitmapHunter hunter in hunters)
                {
                    hunter.Picasso.Complete(hunter);
                }
            });
        }

        private void CompleteAction(UIImage result, Action<UIImage, UIImage> action, LoadedFrom loadedFrom)
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
            private IDownloader<UIImage> m_Downloader;
            private IRequestTransformer<UIImage> m_RequestTransformer;
            private List<RequestHandler> m_RequestHandlers;

            public Builder()
            {
            }

            public Builder Cache(ICache<UIImage> cache)
            {
                m_Cache = cache;
                return this;
            }

            public Builder Downloader(IDownloader<UIImage> downloader)
            {
                m_Downloader = downloader;
                return this;
            }

            public Builder RequestTransformer(IRequestTransformer<UIImage> requestTransformer)
            {
                if (requestTransformer == null)
                {
                    throw new ArgumentNullException("requestTransformer");
                }
                if (m_RequestTransformer != null)
                {
                    throw new InvalidOperationException("Request transformer already set");
                }
                m_RequestTransformer = requestTransformer;
                return this;
            }

            public Builder AddRequestHandler(RequestHandler requestHandler)
            {
                if (requestHandler == null)
                {
                    throw new ArgumentNullException("requestHandler");
                }
                if (m_RequestHandlers == null)
                {
                    m_RequestHandlers = new List<RequestHandler>();
                }
                if (m_RequestHandlers.Contains(requestHandler))
                {
                    throw new InvalidOperationException("RequestHandler already registered.");
                }
                m_RequestHandlers.Add(requestHandler);
                return this;
            }

            public Picasso Build()
            {
                if (m_Cache == null)
                {
                    int cacheSize = IOSUtils.CalculateCacheSize();
                    m_Cache = new LruCache<UIImage>(cacheSize, IOSUtils.SizeOfImage);
                }

                if (m_Downloader == null)
                {
                    m_Downloader = new NSUrlDownloader();
                }

                if (m_RequestTransformer == null)
                {
                    m_RequestTransformer = new DummyRequestTransformer();
                }

                var dispatcher = new Dispatcher(m_Cache, m_Downloader);

                return new Picasso(m_Cache, m_RequestTransformer, m_RequestHandlers, dispatcher);
            }

            public class DummyRequestTransformer : IRequestTransformer<UIImage>
            {
                public Request<UIImage> TransformRequest(Request<UIImage> request)
                {
                    return request;
                }
            }
        }
    }
}
