using Android;
using Android.Annotation;
using Android.Content;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Java.IO;
using Java.Lang;
using Java.Util.Concurrent;
using Process = Android.OS.Process;

namespace PicassoSharp
{
    public class AndroidUtils
    {
        private const long MinDiskCacheSize = 5 * 1024 * 1024;
        private const long MaxDiskCacheSize = 50 * 1024 * 1024;
        private const string PicassoCache = "picasso-cache";

        public static int CalculateMemoryCacheSize(Context context)
        {
            var am = (ActivityManager) context.GetSystemService(Context.ActivityService);
            bool largeHeap = (context.ApplicationInfo.Flags & ApplicationInfoFlags.LargeHeap) != 0;
            int memoryClass = am.MemoryClass;
            if (largeHeap && Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                memoryClass = am.LargeMemoryClass;
            }

            return 1024*1024*memoryClass/7;
        }

        public static int MaxViewBitmapSize
        {
            get
            {
//                if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
//                    return MaxViewBitmapSizeIcs;
                return 2048;
            }
        }

        [TargetApi(Value = (int)BuildVersionCodes.IceCreamSandwich)]
        private static int MaxViewBitmapSizeIcs
        {
            get
            {
                // TODO This sometimes doesn't provide the correct max bitmap size
                // Need to correct this behavior before it can be used
                var c = new Canvas();
                return c.MaximumBitmapHeight > c.MaximumBitmapWidth ? c.MaximumBitmapWidth : c.MaximumBitmapHeight;
            }
        }

        internal class PicassoSharpThreadFactory : Object, IThreadFactory
        {
            public Thread NewThread(IRunnable r)
            {
                return new PicassoSharpThread(r);
            }
        }

        internal class PicassoSharpThread : Thread
        {
            public PicassoSharpThread(IRunnable runnable)
                : base(runnable)
            {
            }

            public override void Run()
            {
                Process.SetThreadPriority(ThreadPriority.Background);
                base.Run();
            }
        }

        public static bool HasPermission(Context context, string permission)
        {
            return context.CheckCallingOrSelfPermission(Manifest.Permission.AccessNetworkState) ==
                   Permission.Granted;
        }

        [TargetApi(Value = (int) BuildVersionCodes.JellyBeanMr1)]
        public static bool IsAirplaneModeOn(Context context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr1)
            {
                return Settings.System.GetInt(context.ContentResolver, Settings.System.AirplaneModeOn, 0) != 0;
            }

            return Settings.Global.GetInt(context.ContentResolver, Settings.Global.AirplaneModeOn, 0) != 0;
        }

        public static int SizeOfBitmap(Bitmap bitmap)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.HoneycombMr1)
            {
                return bitmap.ByteCount;
            }
            return bitmap.RowBytes*bitmap.Height;
        }

        public static File CreateDefaultCacheDir(Context context)
        {
            var cache = new File(context.ApplicationContext.CacheDir, PicassoCache);
            if (!cache.Exists())
            {
                cache.Mkdirs();
            }
            return cache;
        }

        public static long CalculateDiskCacheSize(File cacheDir)
        {
            long size = MinDiskCacheSize;

            try
            {
                var statFs = new StatFs(cacheDir.AbsolutePath);

                long available = 0;
                if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr2)
                {
                    available = statFs.BlockCount*statFs.BlockSize;
                }
                else
                {
                    available = statFs.BlockCountLong * statFs.BlockSizeLong;
                }

                size = available/50;
            }
            catch (IllegalArgumentException) { }

            return Math.Max(Math.Min(size, MaxDiskCacheSize), MinDiskCacheSize);
        }

        public static IDownloader<Bitmap> CreateDefaultDownloader(Context context)
        {
            // For now this just returns a UrlConnectionDownloader
            return new UrlConnectionDownloader(context);
        }

        public static ObjectWrapper<T> Wrap<T>(T value)
        {
            return new ObjectWrapper<T>(value);
        }

        public static T Unwrap<T>(ObjectWrapper<T> value)
        {
            return value.Value;
        }

        public class ObjectWrapper<T> : Object
        {
            private readonly T m_Value;

            public ObjectWrapper(T value)
            {
                m_Value = value;
            }

            public T Value
            {
                get { return m_Value; }
            }
        }
    }
}

