using Android;
using Android.Content;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Java.Lang;
using Java.Util.Concurrent;

namespace PicassoSharp
{
	public class AndroidUtils
	{
		public static int CalculateCacheSize(Context context)
		{
            ActivityManager am = (ActivityManager)context.GetSystemService(Context.ActivityService);
			bool largeHeap = (context.ApplicationInfo.Flags & Android.Content.PM.ApplicationInfoFlags.LargeHeap) != 0;
			int memoryClass = am.MemoryClass;
			if (largeHeap && Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Honeycomb)
			{
				memoryClass = am.LargeMemoryClass;
			}

			return 1024 * 1024 * memoryClass / 7;
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
                Android.OS.Process.SetThreadPriority(ThreadPriority.Background);
                base.Run();
            }
        }

	    public static bool HasPermission(Context context, string permission)
	    {
	        return context.CheckCallingOrSelfPermission(Manifest.Permission.AccessNetworkState) ==
	               Android.Content.PM.Permission.Granted;
	    }

	    public static bool IsAirplaneModeOn(Context context)
        {
            ContentResolver contentResolver = context.ContentResolver;
            return Settings.System.GetInt(contentResolver, Settings.System.AirplaneModeOn, 0) != 0;
	    }

	    public static int SizeOfBitmap(Bitmap bitmap)
	    {
	        if (Build.VERSION.SdkInt >= BuildVersionCodes.HoneycombMr1)
	        {
	            return bitmap.ByteCount;
	        }
	        return bitmap.RowBytes*bitmap.Height;
	    }
	}

}

