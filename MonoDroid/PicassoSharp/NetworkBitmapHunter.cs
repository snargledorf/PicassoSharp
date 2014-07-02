using System.IO;
using Android.Graphics;

namespace PicassoSharp
{
    internal class NetworkBitmapHunter : BitmapHunter
    {
        private const int DefaultRetryCount = 2;

        private readonly IDownloader m_Downloader;
        private int m_RetryCount;

        internal NetworkBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache,
            IDownloader downloader)
            : base(picasso, action, dispatcher, cache)
        {
            m_Downloader = downloader;
            m_RetryCount = DefaultRetryCount;
        }
        
        protected override Bitmap Decode(Request data)
        {
            bool localCacheOnly = m_RetryCount == 0;

            Response response = m_Downloader.Load(data.Uri, localCacheOnly);
            if (response == null)
                return null;

            LoadedFrom = response.Cached ? LoadedFrom.Disk : LoadedFrom.Network;

            Stream stream = response.BitmapStream;
            if (stream == null)
                return null;

            try
            {
                return DecodeStream(stream);
            }
            finally
            {
                Utils.CloseQuietly(stream);
            }
        }

        private Bitmap DecodeStream(Stream stream)
        {
            BitmapFactory.Options options = CreateBitmapOptions(Data);
            bool calculateSize = RequiresInSampleSize(options);

            byte[] bytes = Utils.ToByteArray(stream);
            if (calculateSize)
            {
                BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
                CalculateInSampleSize(Data.TargetWidth, Data.TargetHeight, options);
            }
            return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
        }

        public override bool ShouldRetry(bool airplaneMode, Android.Net.NetworkInfo info)
        {
            bool hasRetries = m_RetryCount > 0;
            if (!hasRetries)
                return false;

            m_RetryCount--;
            return info == null || info.IsConnectedOrConnecting;
        }
    }
}

