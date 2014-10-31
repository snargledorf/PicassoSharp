using System;
using System.Collections.Generic;
using Android;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Java.Util;
using Java.Util.Concurrent;

namespace PicassoSharp
{
	internal class Dispatcher
    {
        private const int BatchDelay = 200;
        private const int AirplaneModeOn = 1;
        private const int AirplaneModeOff = 0;

        const int RequestSubmit = 1;
        const int RequestCancel = 2;
        const int RequestGced = 3;
        const int HunterComplete = 4;
        const int HunterRetry = 5;
        const int HunterDecodeFailed = 6;
        const int HunterDelayNextBatch = 7;
        const int NetworkStateChange = 8;
        const int AirplaneModeChange = 9;

        private readonly DispatcherThread m_DipatcherThread;
        private readonly DispatcherHandler m_Handler;
        private readonly Handler m_MainThreadHandler;
        private readonly Context m_Context;
        private readonly ICache<Bitmap> m_Cache;
	    private readonly IDownloader m_Downloader;
	    private readonly IExecutorService m_Service;
        private readonly NetworkBroadcastReceiver m_Receiver;
        private readonly Dictionary<String, BitmapHunter> m_Hunters = new Dictionary<String, BitmapHunter>();
        private readonly List<BitmapHunter> m_Batch = new List<BitmapHunter>();
	    private bool m_AirplaneMode;
	    private NetworkInfo m_NetworkInfo;

	    public IDownloader Downloader
	    {
	        get { return m_Downloader; }
	    }

	    internal Dispatcher(Context context, Handler mainThreadHandler, IExecutorService service, ICache<Bitmap> cache, IDownloader downloader)
        {
            m_DipatcherThread = new DispatcherThread();
	        m_DipatcherThread.Start();
	        m_Handler = new DispatcherHandler(m_DipatcherThread.Looper, this);
	        m_Context = context;
            m_MainThreadHandler = mainThreadHandler;
            m_Cache = cache;
	        m_Downloader = downloader;
	        m_Service = service;
            m_AirplaneMode = AndroidUtils.IsAirplaneModeOn(m_Context);
	        m_Receiver = new NetworkBroadcastReceiver(this);
	        m_Receiver.Register();
        }

	    internal void Shutdown()
	    {
	        m_Service.Shutdown();
	        m_DipatcherThread.Quit();
	        m_Receiver.Unregister();
	    }

	    internal void DispatchSubmit(Action action)
	    {
	        m_Handler.SendMessage(m_Handler.ObtainMessage(RequestSubmit, action));
        }

        internal void DispatchCancel(Action action)
        {
            m_Handler.SendMessage(m_Handler.ObtainMessage(RequestCancel, action));
        }

        internal void DispatchComplete(BitmapHunter hunter)
        {
            m_Handler.SendMessage(m_Handler.ObtainMessage(HunterComplete, hunter));
        }

        public void DispatchRetry(BitmapHunter hunter)
        {
            m_Handler.SendMessage(m_Handler.ObtainMessage(HunterRetry, hunter));
        }

        internal void DispatchFailed(BitmapHunter hunter)
        {
            m_Handler.SendMessage(m_Handler.ObtainMessage(HunterDecodeFailed, hunter));
        }

	    private void DispatchAirplaneModeChange(bool airplaneMode)
	    {
	        m_Handler.SendMessage(m_Handler.ObtainMessage(AirplaneModeChange,
	            airplaneMode ? AirplaneModeOn : AirplaneModeOff, 0));
	    }

	    private void DispatchNetworkStateChange(NetworkInfo info)
	    {
	        m_Handler.SendMessage(m_Handler.ObtainMessage(NetworkStateChange, info));
	    }

	    void PerformSubmit(Action action)
	    {
            BitmapHunter hunter;
            if (m_Hunters.TryGetValue(action.Key, out hunter))
            {
                hunter.Attach(action);
                return;
            }

	        if (m_Service.IsShutdown)
	        {
                return;
	        }

            hunter = BitmapHunter.ForRequest(action.Picasso, action, this, m_Cache);
            hunter.Future = m_Service.Submit(hunter);
            m_Hunters.Add(action.Key, hunter);
	    }

	    void PerformCancel(Action action)
	    {
	        string key = action.Key;
	        BitmapHunter hunter;
            m_Hunters.TryGetValue(key, out hunter);
            if (hunter != null)
            {
                hunter.Detach(action);
                if (hunter.Cancel())
                {
                    m_Hunters.Remove(key);
                }
            }
	    }

	    void PerformComplete(BitmapHunter hunter)
	    {
	        if (!hunter.SkipCache)
            {
                m_Cache.Set(hunter.Key, hunter.Result);
	        }

            m_Hunters.Remove(hunter.Key);
            Batch(hunter);
	    }

        private void PerformRetry(BitmapHunter hunter)
        {
            if (hunter.Cancelled)
                return;

            if (m_Service.IsShutdown)
            {
                PerformError(hunter);
                return;
            }

            bool hasConnectivity = m_NetworkInfo != null && m_NetworkInfo.IsConnectedOrConnecting;
            bool shouldRetryHunter = hunter.ShouldRetry(m_AirplaneMode, m_NetworkInfo);

            if (shouldRetryHunter)
            {
                if (hasConnectivity)
                {
                    hunter.Future = m_Service.Submit(hunter);
                }
                else
                {
                    PerformError(hunter);
                }
            }
            else
            {
                PerformError(hunter);
            }
        }

	    private void PerformBatchComplete()
	    {
	        var copy = new ArrayList(m_Batch);
            m_Batch.Clear();
            m_MainThreadHandler.SendMessage(m_MainThreadHandler.ObtainMessage(Picasso.BatchComplete, copy));
	    }

	    void PerformError(BitmapHunter hunter)
	    {
            m_Hunters.Remove(hunter.Key);
            Batch(hunter);
        }

        private void PerformNetworkStateChange(NetworkInfo info)
        {
            m_NetworkInfo = info;

            var service = m_Service as PicassoExecutorService;
            if (service != null)
            {
                service.AdujstThreadCount(info);
            }
        }

	    private void PerformAirplaneModeChange(bool airplaneMode)
        {
            m_AirplaneMode = airplaneMode;
        }

        void Batch(BitmapHunter hunter)
        {
            if (hunter.Cancelled)
            {
                return;
            }

            m_Batch.Add(hunter);
            if (!m_Handler.HasMessages(HunterDelayNextBatch))
            {
                m_Handler.SendEmptyMessageDelayed(HunterDelayNextBatch, BatchDelay);
            }
        }

	    private class DispatcherHandler : Handler
	    {
	        private readonly Dispatcher m_Dispatcher;

	        public DispatcherHandler(Looper looper, Dispatcher dispatcher)
	            : base(looper)
	        {
	            m_Dispatcher = dispatcher;
	        }

	        public override void HandleMessage(Message msg)
	        {
	            switch (msg.What)
	            {
	                case RequestSubmit:
	                {
	                    var action = (Action) msg.Obj;
	                    m_Dispatcher.PerformSubmit(action);
	                }
	                    break;
	                case RequestCancel:
	                {
	                    var action = (Action) msg.Obj;
	                    m_Dispatcher.PerformCancel(action);
	                }
	                    break;
	                case HunterComplete:
	                {
	                    var hunter = (BitmapHunter) msg.Obj;
	                    m_Dispatcher.PerformComplete(hunter);
	                }
	                    break;
                    case HunterRetry:
                    {
                      var hunter = (BitmapHunter) msg.Obj;
                      m_Dispatcher.PerformRetry(hunter);
                      break;
                    }
	                case HunterDecodeFailed:
	                {
	                    var hunter = (BitmapHunter) msg.Obj;
	                    m_Dispatcher.PerformError(hunter);
	                }
	                    break;
	                case HunterDelayNextBatch:
	                {
	                    m_Dispatcher.PerformBatchComplete();
	                }
	                    break;
	                case NetworkStateChange:
	                {
	                    var info = (NetworkInfo) msg.Obj;
	                    m_Dispatcher.PerformNetworkStateChange(info);
	                }
	                    break;
	                case AirplaneModeChange:
	                {
	                    m_Dispatcher.PerformAirplaneModeChange(msg.Arg1 == AirplaneModeOn);
	                }
	                    break;
//                    default:
//          Picasso.HANDLER.post(new Runnable() {
//            @Override public void run() {
//              throw new AssertionError("Unknown handler message received: " + msg.what);
//            }
//          });
	            }
	        }
	    }

	    private class DispatcherThread : HandlerThread
        {
            public DispatcherThread()
                : base(Utils.ThreadPrefix, (int)ThreadPriority.Background)
            {
            }
        }

        private class NetworkBroadcastReceiver : BroadcastReceiver
        {
            private const string ExtraAirplaneState = "state";

            private readonly Dispatcher m_Dispatcher;

            public NetworkBroadcastReceiver(Dispatcher dispatcher)
            {
                m_Dispatcher = dispatcher;
            }

            public void Register()
            {
                bool shouldScanState = m_Dispatcher.m_Service is PicassoExecutorService &&
                                       AndroidUtils.HasPermission(m_Dispatcher.m_Context,
                                           Manifest.Permission.AccessNetworkState);
                var intentFilter = new IntentFilter();
                intentFilter.AddAction(Intent.ActionAirplaneModeChanged);
                if (shouldScanState)
                {
                    intentFilter.AddAction(ConnectivityManager.ConnectivityAction);
                }
                m_Dispatcher.m_Context.RegisterReceiver(this, intentFilter);
            }

            public void Unregister()
            {
                m_Dispatcher.m_Context.UnregisterReceiver(this);
            }

            #region implemented abstract members of BroadcastReceiver

            public override void OnReceive(Context context, Intent intent)
            {
                // On some versions of Android this may be called with a null Intent,
                // also without extras (getExtras() == null), in such case we use defaults.
                if (intent == null)
                {
                    return;
                }

                String action = intent.Action;
                if (Intent.ActionAirplaneModeChanged.Equals(action))
                {
                    if (intent.HasExtra(ExtraAirplaneState))
                    {
                        return;
                    }
                    m_Dispatcher.DispatchAirplaneModeChange(intent.GetBooleanExtra(ExtraAirplaneState, false));
                }
                else if (ConnectivityManager.ConnectivityAction.Equals(action))
                {
                    var connectivityManager = (ConnectivityManager)m_Dispatcher.m_Context.GetSystemService(Context.ConnectivityService);
                    m_Dispatcher.DispatchNetworkStateChange(connectivityManager.ActiveNetworkInfo);
                }
            }

            #endregion
        }
    }
}

