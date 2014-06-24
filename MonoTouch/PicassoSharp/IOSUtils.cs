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
    }
}