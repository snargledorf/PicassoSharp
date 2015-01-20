using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;
using Java.IO;
using Java.Util;
using Java.Util.Concurrent;
using IList = System.Collections.IList;

namespace PicassoSharp
{
    public sealed class Picasso : IPicasso<Bitmap, Drawable>, IDisposable
    {
        public interface IListener
        {
            void OnImageLoadFailed(Picasso picasso, Uri uri, Exception exception);
        }

        private const int RequestGced = 0;
        public const int BatchComplete = 1;

        public static readonly Handler Handler = new PicassoSharpHandler();

        private static Picasso s_Instance;

        private readonly Context m_Context;
        private readonly IExecutorService m_Service;
        private readonly ICache<Bitmap> m_Cache;
        private readonly IRequestTransformer<Bitmap> m_RequestTransformer;
        private readonly Dispatcher m_Dispatcher;
        private readonly IListener m_Listener;
        private readonly IList<IRequestHandler<Bitmap>> m_RequestHandlers;
        private readonly WeakHashtable<object, Action<Bitmap, Drawable>> m_TargetToAction;
        private readonly WeakHashtable<ImageView, DeferredRequestCreator> m_TargetToDeferredRequestCreator;
        
        private bool m_Disposed;

        internal Context Context
        {
            get { return m_Context; }
        }

        internal ICache<Bitmap> Cache
        {
            get { return m_Cache; }
        }

        internal Dispatcher Dispatcher
        {
            get { return m_Dispatcher; }
        }

        public IList<IRequestHandler<Bitmap>> RequestHandlers
        {
            get { return m_RequestHandlers; }
        }

        private Picasso(Context context, ICache<Bitmap> cache, IRequestTransformer<Bitmap> requestTransformer, List<RequestHandler> extraRequestHandlers, IExecutorService service, Dispatcher dispatcher, IListener listener)
        {
            m_Context = context;
            m_Cache = cache;
            m_RequestTransformer = requestTransformer;

            const int builtInHandlersCount = 3;
            int extraHandlersCount = extraRequestHandlers != null ? extraRequestHandlers.Count : 0;
            var allRequestHandlers = new List<IRequestHandler<Bitmap>>(builtInHandlersCount + extraHandlersCount);

            // ResourceRequestHandler needs to be the first in the list to avoid
            // forcing other RequestHandlers to perform null checks on request.uri
            // to cover the (request.resourceId != 0) case.
            allRequestHandlers.Add(new ResourceRequestHandler(context));
            if (extraRequestHandlers != null)
            {
                allRequestHandlers.AddRange(allRequestHandlers);
            }
            allRequestHandlers.Add(new ContentStreamRequestHandler(context));
            allRequestHandlers.Add(new FileRequestHandler(context));
            allRequestHandlers.Add(new NetworkRequestHandler(dispatcher.Downloader));
            m_RequestHandlers = new ReadOnlyCollection<IRequestHandler<Bitmap>>(allRequestHandlers);

            m_Service = service;
            m_Dispatcher = dispatcher;
            m_Listener = listener;
            m_TargetToAction = new WeakHashtable<object, Action<Bitmap, Drawable>>();
            m_TargetToDeferredRequestCreator = new WeakHashtable<ImageView, DeferredRequestCreator>();
        }

        public RequestCreator Load(string path)
        {
            if (path == null)
            {
                return new RequestCreator(this, null, 0);
            }
            if (path.Trim().Length == 0)
            {
                throw new ArgumentException("Path must not be empty");
            }
            return Load(new Uri(path));
        }

        public RequestCreator Load(Uri uri)
        {
            return new RequestCreator(this, uri, 0);
        }

        public RequestCreator Load(int resourceId)
        {
            return new RequestCreator(this, null, resourceId);
        }

        public RequestCreator Load(File file)
        {
            if (file == null)
            {
                return new RequestCreator(this, null, 0);
            }
            return Load(file.AbsolutePath);
        }

        public bool IsShutdown { get; private set; }

        public void Shutdown()
        {
            if (this == s_Instance)
                throw new NotSupportedException("Default instance cannot be shutdown.");

            if (IsShutdown)
                return;

            m_Cache.Clear();

            m_Service.Shutdown();

            foreach (DeferredRequestCreator deferredRequestCreator in m_TargetToDeferredRequestCreator.Values)
            {
                deferredRequestCreator.Cancel();
            }

            m_TargetToDeferredRequestCreator.Clear();
            
            IsShutdown = true;
        }

        void IPicasso<Bitmap, Drawable>.CancelExistingRequest(Object target)
        {
            PerformCancelExistingRequest(target);
        }

        private void PerformCancelExistingRequest(Object target)
        {
            Action<Bitmap, Drawable> action;
            if (m_TargetToAction.TryGetValue(target, out action))
            {
                action.Cancel();
                m_Dispatcher.DispatchCancel(action);
            }
            m_TargetToAction.Remove(target);

            var imageViewTarget = target as ImageView;
            if (imageViewTarget == null)
                return;

            DeferredRequestCreator deferredRequestCreator;
            m_TargetToDeferredRequestCreator.TryGetValue(imageViewTarget, out deferredRequestCreator);
            if (deferredRequestCreator != null)
            {
                deferredRequestCreator.Cancel();
            }
            m_TargetToDeferredRequestCreator.Remove(imageViewTarget);
        }

        private void LinkTargetToAction(Object target, Action<Bitmap, Drawable> action)
        {
            m_TargetToAction.Add(target, action);
        }

        internal void Defer(ImageView target, DeferredRequestCreator deferredRequestCreator)
        {
            m_TargetToDeferredRequestCreator.Add(target, deferredRequestCreator);
        }

        internal void EnqueueAndSubmit(Action<Bitmap, Drawable> action)
        {
            var target = action.Target;
            if (target != null)
            {
                PerformCancelExistingRequest(target);
                LinkTargetToAction(target, action);
            }
            Submit(action);
        }

        internal void Submit(Action<Bitmap, Drawable> action)
        {
            m_Dispatcher.DispatchSubmit(action);
        }

        internal Bitmap QuickMemoryCacheCheck(String key)
        {
            Bitmap cached = m_Cache.Get(key);
            return cached;
        }

        public void CancelRequest(ImageView target)
        {
            PerformCancelExistingRequest(target);
        }

        public void CancelRequest(ITarget<Bitmap, Drawable, Drawable> target)
        {
            PerformCancelExistingRequest(target);
        }

        void IPicasso<Bitmap, Drawable>.Complete(IBitmapHunter<Bitmap, Drawable> hunter)
        {
            Uri uri = hunter.Data.Uri;
            Action<Bitmap, Drawable> action = hunter.Action;
            List<Action<Bitmap, Drawable>> additionalActions = hunter.Actions;
            Bitmap result = hunter.Result;
            LoadedFrom loadedFrom = hunter.LoadedFrom;
            Exception exception = hunter.Exception;

            if (action != null)
            {
                CompleteAction(result, action, loadedFrom);
            }

            if (additionalActions != null)
            {
                foreach (Action<Bitmap, Drawable> additionalAction in additionalActions)
                {
                    CompleteAction(result, additionalAction, loadedFrom);
                }
            }

            if (m_Listener != null && exception != null)
            {
                m_Listener.OnImageLoadFailed(this, uri, exception);
            }
        }

        void IPicasso<Bitmap, Drawable>.RunOnPicassoThread(Action action)
        {
            Handler.Post(action);
        }

        private void CompleteAction(Bitmap result, Action<Bitmap, Drawable> action, LoadedFrom loadedFrom)
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

        public static Picasso With(Context context)
        {
            return s_Instance ?? (s_Instance = new Builder(context).Build());
        }

        public class Builder
        {
            private readonly Context m_Context;
            private ICache<Bitmap> m_Cache;
            private IExecutorService m_Service;
            private IDownloader<Bitmap> m_Downloader;
            private IListener m_Listener;
            private IRequestTransformer<Bitmap> m_RequestTransformer;
            private List<RequestHandler> m_RequestHandlers;

            public Builder(Context context)
            {
                m_Context = context.ApplicationContext;
            }

            public Builder Cache(ICache<Bitmap> cache)
            {
                if (cache == null)
                {
                    throw new ArgumentNullException("cache");
                }
                if (m_Cache != null)
                {
                    throw new InvalidOperationException("Cache already set");
                }
                m_Cache = cache;
                return this;
            }

            public Builder Executor(IExecutorService service)
            {
                if (service == null)
                {
                    throw new ArgumentNullException("service");
                }
                if (m_Service != null)
                {
                    throw new InvalidOperationException("Executor service already set");
                }
                m_Service = service;
                return this;
            }

            public Builder Downloader(IDownloader<Bitmap> downloader)
            {
                if (downloader == null)
                {
                    throw new ArgumentNullException("downloader");
                }
                if (m_Downloader != null)
                {
                    throw new InvalidOperationException("Downloader already set");
                }

                m_Downloader = downloader;
                return this;
            }

            public Builder Listener(IListener listener)
            {
                if (listener == null)
                {
                    throw new ArgumentNullException("listener");
                }
                if (m_Listener != null)
                {
                    throw new InvalidOperationException("Listener already set");
                }
                m_Listener = listener;
                return this;
            }

            public Builder RequestTransformer(IRequestTransformer<Bitmap> requestTransformer)
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
                    int cacheSize = AndroidUtils.CalculateMemoryCacheSize(m_Context);
                    m_Cache = new LruCache<Bitmap>(cacheSize, AndroidUtils.SizeOfBitmap);
                }

                if (m_Service == null)
                {
                    m_Service = new PicassoExecutorService();
                }

                if (m_Downloader == null)
                {
                    m_Downloader = AndroidUtils.CreateDefaultDownloader(m_Context);
                }

                if (m_RequestTransformer == null)
                {
                    m_RequestTransformer = new DummyRequestTransformer();
                }

                var dispatcher = new Dispatcher(m_Context, Handler, m_Service, m_Cache, m_Downloader);

                return new Picasso(m_Context, m_Cache, m_RequestTransformer, m_RequestHandlers, m_Service, dispatcher, m_Listener);
            }

            public class DummyRequestTransformer : IRequestTransformer<Bitmap>
            {
                public Request<Bitmap> TransformRequest(Request<Bitmap> request)
                {
                    return request;
                }
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
                        var hunters = (ArrayList) msg.Obj;
                        for (int i = 0; i < hunters.Size(); i++)
                        {
                            var hunter = (BitmapHunter) hunters.Get(i);
                            hunter.Picasso.Complete(hunter);
                        }
                        break;
                    case RequestGced:
                    {
                        var actionWrapper = (AndroidUtils.ObjectWrapper<Action<Bitmap, Drawable>>) msg.Obj;
                        actionWrapper.Value.Picasso.CancelExistingRequest(actionWrapper.Value.Target);
                    }
                        break;
                }
            }
        }

        internal Request<Bitmap> TransformRequest(Request<Bitmap> request)
        {
            Request<Bitmap> transformed = m_RequestTransformer.TransformRequest(request);
            if (transformed == null)
            {
                throw new InvalidOperationException("Request transformer "
                    + m_RequestTransformer.GetType().Name
                    + " returned null for "
                    + request);
            }
            return transformed;
        }
    }
}
