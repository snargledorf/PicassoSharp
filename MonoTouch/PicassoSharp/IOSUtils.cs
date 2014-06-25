using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
    public class IOSUtils
    {
        public static int CalculateCacheSize()
        {
            return 1024*10*10;
        }

        public static bool IsAirplaneModeOn()
        {
            // TODO Detect airplane mode
            return false;
        }

        public static int SizeOfImage(UIImage arg)
        {
            return arg.CGImage.BytesPerRow*arg.CGImage.Height;
        }
    }
}