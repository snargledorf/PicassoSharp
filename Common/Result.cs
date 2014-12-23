namespace PicassoSharp
{
    public class Result<TBitmap>
    {
        private readonly LoadedFrom m_LoadedFrom;
        private readonly TBitmap m_Bitmap;
        private readonly int m_ExifOrientation;

        public TBitmap Bitmap
        {
            get { return m_Bitmap; }
        }

        public LoadedFrom LoadedFrom
        {
            get { return m_LoadedFrom; }
        }

        public int ExifOrientation
        {
            get { return m_ExifOrientation; }
        }

        public Result(TBitmap bitmap, LoadedFrom loadedFrom) : this(bitmap, loadedFrom, 0) { }

        public Result(TBitmap bitmap, LoadedFrom loadedFrom, int exifOrientation)
        {
            m_Bitmap = bitmap;
            m_LoadedFrom = loadedFrom;
            m_ExifOrientation = exifOrientation;
        }
    }
}