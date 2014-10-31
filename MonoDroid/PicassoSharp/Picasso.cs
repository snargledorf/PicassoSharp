using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Java.Util;
using Java.Util.Concurrent;
using IList = System.Collections.IList;

namespace PicassoSharp
{
    public sealed class Picasso : IDisposable
    {
        public interface IListener
        {
            void OnImageLoadFailed(Picasso picasso, Uri uri, Exception exception);
        }

        public interface IRequestTransformer
        {
            Request TransformRequest(Request request);
        }

        private const int RequestGced = 0;
        public const int BatchComplete = 1;

        public static readonly Handler Handler = new PicassoSharpHandler();

        private static Picasso s_Instance;

        private readonly Context m_Context;
        private readonly IExecutorService m_Service;
        private readonly ICache<Bitmap> m_Cache;
        private readonly IRequestTransformer m_RequestTransformer;
        private readonly IList m_RequestHandlers;
        private readonly Dispatcher m_Dispatcher;
        private readonly IListener m_Listener;
        private readonly WeakHashtable<object, Action> m_TargetToAction;
        private readonly WeakHashtable<ImageView, DeferredRequestCreator> m_TargetToDeferredRequestCreator;
        
        private bool m_Disposed;

        public Context Context
        {
            get { return m_Context; }
        }

        public IList RequestHandlers
        {
            get { return m_RequestHandlers; }
        }

        private Picasso(Context context, ICache<Bitmap> cache, IRequestTransformer requestTransformer, List<RequestHandler> extraRequestHandlers, IExecutorService service, Dispatcher dispatcher, IListener listener)
        {
            m_Context = context;
            m_Cache = cache;
            m_RequestTransformer = requestTransformer;

            const int builtInHandlersCount = 3;
            int extraHandlersCount = extraRequestHandlers != null ? extraRequestHandlers.Count : 0;
            var allRequestHandlers = new List<RequestHandler>(builtInHandlersCount + extraHandlersCount);

            // ResourceRequestHandler needs to be the first in the list to avoid
            // forcing other RequestHandlers to perform null checks on request.uri
            // to cover the (request.resourceId != 0) case.
            allRequestHandlers.Add(new ResourceRequestHandler(context));
            if (extraRequestHandlers != null)
            {
                allRequestHandlers.AddRange(allRequestHandlers);
            }
            allRequestHandlers.Add(new FileRequestHandler());
            allRequestHandlers.Add(new NetworkRequestHandler(dispatcher.Downloader));
            m_RequestHandlers = Collections.UnmodifiableList(allRequestHandlers);

            m_Service = service;
            m_Dispatcher = dispatcher;
            m_Listener = listener;
            m_TargetToAction = new WeakHashtable<object, Action>();
            m_TargetToDeferredRequestCreator = new WeakHashtable<ImageView, DeferredRequestCreator>();
        }

        public ICache<Bitmap> Cache
        {
            get { return m_Cache; }
        }

        public RequestCreator Load(string path)
        {
            return Load(new Uri(path));
        }

        public RequestCreator Load(Uri uri)
        {
#warning Remove this once the server has been updated!!!
            if (uri.Scheme == Uri.UriSchemeFtp)
            {
                uri = new UriBuilder(uri)
                {
                    Scheme = Uri.UriSchemeHttp,
                    Port = -1
                }.Uri;
            }

            return new RequestCreator(this, uri);
        }

        public RequestCreator Load(int resourceId)
        {
            return new RequestCreator(this, resourceId);
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

        private void CancelExistingRequest(Object target)
        {
            Action action;
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

        private void LinkTargetToAction(Object target, Action action)
        {
            m_TargetToAction.Add(target, action);
        }

        internal void Defer(ImageView target, DeferredRequestCreator deferredRequestCreator)
        {
            m_TargetToDeferredRequestCreator.Add(target, deferredRequestCreator);
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
            Uri uri = hunter.Data.Uri;
            Action action = hunter.Action;
            List<Action> additionalActions = hunter.Actions;
            Bitmap result = hunter.Result;
            LoadedFrom loadedFrom = hunter.LoadedFrom;
            Exception exception = hunter.Exception;

            if (action != null)
            {
                CompleteAction(result, action, loadedFrom);
            }

            if (additionalActions != null)
            {
                foreach (Action additionalAction in additionalActions)
                {
                    CompleteAction(result, additionalAction, loadedFrom);
                }
            }

            if (m_Listener != null && exception != null)
            {
                m_Listener.OnImageLoadFailed(this, uri, exception);
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

        public static Picasso With(Context context)
        {
            return s_Instance ?? (s_Instance = new Builder(context).Build());
        }

        public class Builder
        {
            private readonly Context m_Context;
            private ICache<Bitmap> m_Cache;
            private IExecutorService m_Service;
            private IDownloader m_Downloader;
            private IListener m_Listener;
            private IRequestTransformer m_RequestTransformer;
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
                    throw new IllegalStateException("Cache already set");
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
                    throw new IllegalStateException("Executor service already set");
                }
                m_Service = service;
                return this;
            }

            public Builder Downloader(IDownloader downloader)
            {
                if (downloader == null)
                {
                    throw new ArgumentNullException("downloader");
                }
                if (m_Downloader != null)
                {
                    throw new IllegalStateException("Downloader already set");
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
                    throw new IllegalStateException("Listener already set");
                }
                m_Listener = listener;
                return this;
            }

            public Builder RequestTransformer(IRequestTransformer requestTransformer)
            {
                if (requestTransformer == null)
                {
                    throw new ArgumentNullException("requestTransformer");
                }
                if (m_RequestTransformer != null)
                {
                    throw new IllegalStateException("Request transformer already set");
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
                    throw new IllegalStateException("RequestHandler already registered.");
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
                    m_Downloader = Utils.CreateDefaultDownloader(m_Context);
                }

                if (m_RequestTransformer == null)
                {
                    m_RequestTransformer = new DummyRequestTransformer();
                }

                var dispatcher = new Dispatcher(m_Context, Handler, m_Service, m_Cache, m_Downloader);

                return new Picasso(m_Context, m_Cache, m_RequestTransformer, m_RequestHandlers, m_Service, dispatcher, m_Listener);
            }

            public class DummyRequestTransformer : IRequestTransformer
            {
                public Request TransformRequest(Request request)
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
                        var action = (Action) msg.Obj;
                        action.Picasso.CancelExistingRequest(action.Target);
                    }
                        break;
                }
            }
        }
    }
}
