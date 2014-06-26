using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Java.Lang;
using Java.Lang.Ref;
using Java.Util;
using Java.Util.Concurrent;
using Exception = System.Exception;
using Object = System.Object;
using Process = Android.OS.Process;
using RequestWeakReference = PicassoSharp.Action.RequestWeakReference<Java.Lang.Object>;

namespace PicassoSharp
{
    public sealed class Picasso : IDisposable
    {
        public interface IListener
        {
            void OnImageLoadFailed(Picasso picasso, Uri uri, Exception exception);
        }

        private const int RequestGced = 0;
        public const int BatchComplete = 1;

        private static readonly Handler Handler = new PicassoSharpHandler();

        private static Picasso s_Instance;

        private readonly Context m_Context;
        private readonly IExecutorService m_Service;
        private readonly ICache<Bitmap> m_Cache;
        private readonly Dispatcher m_Dispatcher;
        private readonly IListener m_Listener;
        private readonly CleanupThread m_CleanupThread;
        private readonly ConditionalWeakTable<object, Action> m_TargetToAction;

        public ReferenceQueue ReferenceQueue { get; private set; }

        private bool m_Disposed;

        public Context Context
        {
            get { return m_Context; }
        }

        private Picasso(Context context, ICache<Bitmap> cache, IExecutorService service, Dispatcher dispatcher, IListener listener)
        {
            m_Context = context;
            m_Cache = cache;
            m_Service = service;
            m_Dispatcher = dispatcher;
            m_Listener = listener;
            m_TargetToAction = new ConditionalWeakTable<object, Action>();
            ReferenceQueue = new ReferenceQueue();
            m_CleanupThread = new CleanupThread(ReferenceQueue, Handler);
            m_CleanupThread.Start();
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

            m_CleanupThread.Shutdown();

            m_Service.Shutdown();

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

        public void CancelRequest(Target target)
        {
            CancelExistingRequest(target);
        }

        private void Complete(BitmapHunter hunter)
        {
            Uri uri = hunter.Data.Uri;
            Action action = hunter.Action;
            List<Action> actions = hunter.Actions;
            Bitmap result = hunter.Result;
            LoadedFrom loadedFrom = hunter.LoadedFrom;
            Exception exception = hunter.Exception;

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

        private class CleanupThread : Thread
        {
            private readonly ReferenceQueue m_ReferenceQueue;
            private readonly Handler m_Handler;

            internal CleanupThread(ReferenceQueue referenceQueue, Handler handler)
            {
                m_ReferenceQueue = referenceQueue;
                m_Handler = handler;
                Daemon = true;
                Name = Utils.ThreadPrefix + "refQueue";
            }

            public override void Run()
            {
                Process.SetThreadPriority(ThreadPriority.Background);
                while (true)
                {
                    try
                    {
                        var remove = (RequestWeakReference) m_ReferenceQueue.Remove();
                        m_Handler.SendMessage(m_Handler.ObtainMessage(RequestGced, remove.Action));
                    }
                    catch (InterruptedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_Handler.Post(() =>
                        {
                            throw ex;
                        });
                        break;
                    }
                }
            }

            public void Shutdown()
            {
                Interrupt();
            }
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

            public Builder(Context context)
            {
                m_Context = context.ApplicationContext;
            }

            public Builder Cache(ICache<Bitmap> cache)
            {
                m_Cache = cache;
                return this;
            }

            public Builder Executor(IExecutorService service)
            {
                m_Service = service;
                return this;
            }

            public Builder Downloader(IDownloader downloader)
            {
                m_Downloader = downloader;
                return this;
            }

            public Builder Listener(IListener listener)
            {
                m_Listener = listener;
                return this;
            }

            public Picasso Build()
            {
                if (m_Cache == null)
                {
                    int cacheSize = AndroidUtils.CalculateCacheSize(m_Context);
                    m_Cache = new LruCache<Bitmap>(cacheSize, AndroidUtils.SizeOfBitmap);
                }

                if (m_Service == null)
                {
                    m_Service = new PicassoExecutorService();
                }

                if (m_Downloader == null)
                {
                    m_Downloader = new WebClientDownloader();
                }

                var dispatcher = new Dispatcher(m_Context, Handler, m_Service, m_Cache, m_Downloader);

                return new Picasso(m_Context, m_Cache, m_Service, dispatcher, m_Listener);
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
                        ArrayList hunters = (ArrayList) msg.Obj;
                        for (int i = 0; i < hunters.Size(); i++)
                        {
                            BitmapHunter hunter = (BitmapHunter) hunters.Get(i);
                            hunter.Picasso.Complete(hunter);
                        }
                        break;
                    case RequestGced:
                    {
                        Action action = (Action) msg.Obj;
                        action.Picasso.CancelExistingRequest(action.Target);
                    }
                        break;
                }
            }
        }
    }
}
