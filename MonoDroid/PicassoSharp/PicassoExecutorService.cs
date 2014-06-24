using Android.Net;
using Java.Lang;
using Java.Util.Concurrent;

namespace PicassoSharp
{
	public class PicassoExecutorService : ThreadPoolExecutor
    {
        private const int DefaultThreadCount = 3;

        public PicassoExecutorService()
            : base(DefaultThreadCount, DefaultThreadCount, 0, TimeUnit.Milliseconds, new LinkedBlockingQueue(), new AndroidUtils.PicassoSharpThreadFactory())
        {
        }

        public void AdujstThreadCount(NetworkInfo netInfo)
        {
            if (netInfo == null || !netInfo.IsConnectedOrConnecting)
            {
                this.ThreadCount = DefaultThreadCount;
                return;
            }
            switch (netInfo.Type)
            {
                case ConnectivityType.Wifi:
                case ConnectivityType.Wimax:
                case ConnectivityType.Ethernet:
                    this.ThreadCount = 4;
                    break;

                case ConnectivityType.Mobile:
                    switch ((Android.Telephony.NetworkType)netInfo.Subtype)
                    {
                        case Android.Telephony.NetworkType.Lte:
                        case Android.Telephony.NetworkType.Hspap:
                        case Android.Telephony.NetworkType.Ehrpd:
                            this.ThreadCount = 3;
                            break;

                        case Android.Telephony.NetworkType.Umts:
                        case Android.Telephony.NetworkType.Cdma:
                        case Android.Telephony.NetworkType.Evdo0:
                        case Android.Telephony.NetworkType.EvdoA:
                        case Android.Telephony.NetworkType.EvdoB:
                            this.ThreadCount = 2;
                            break;

                        case Android.Telephony.NetworkType.Gprs:
                        case Android.Telephony.NetworkType.Edge:
                            this.ThreadCount = 1;
                            break;

                        default:
                            this.ThreadCount = DefaultThreadCount;
                            break;
                    }
                    break;

                default:
                    this.ThreadCount = DefaultThreadCount;
                    break;
            }
        }

        private int ThreadCount
        {
            set
            {
                CorePoolSize = value;
                MaximumPoolSize = value;
            }
        }
    }
}

