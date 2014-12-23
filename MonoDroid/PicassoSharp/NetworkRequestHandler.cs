using System.IO;
using Android.Graphics;

namespace PicassoSharp
{
    internal class NetworkRequestHandler : RequestHandler
    {
        private const int DefaultRetryCount = 2;
        
        private readonly IDownloader<Bitmap> m_Downloader;
        private int m_RetryCount;

        public override int RetryCount
        {
            get { return DefaultRetryCount; }
        }

        internal NetworkRequestHandler(IDownloader<Bitmap> downloader)
        {
            m_Downloader = downloader;
        }

        public override bool ShouldRetry(bool airplaneMode, Android.Net.NetworkInfo info)
        {
            return info == null || info.IsConnectedOrConnecting;
        }

        public override bool SupportsReplay
        {
            get { return true; }
        }

        public override bool CanHandleRequest(Request<Bitmap> data)
        {
            string schema = data.Uri.Scheme;
            return System.Uri.UriSchemeHttp.Equals(schema) || System.Uri.UriSchemeHttps.Equals(schema);
        }

        public override Result<Bitmap> Load(Request<Bitmap> data)
        {
            Response<Bitmap> response = m_Downloader.Load(data.Uri, false);
            if (response == null)
                return null;

            var loadedFrom = response.Cached ? LoadedFrom.Disk : LoadedFrom.Network;

            Bitmap bitmap = response.Bitmap;
            if (bitmap != null)
                return new Result<Bitmap>(bitmap, loadedFrom);

            Stream stream = response.Stream;
            if (stream == null)
                return null;

            if (response.ContentLength == 0)
            {
                Utils.CloseQuietly(stream);
                throw new IOException("Received response with 0 content-length header.");
            }

            try
            {
                return new Result<Bitmap>(DecodeStream(stream, data), loadedFrom);
            }
            finally
            {
                Utils.CloseQuietly(stream);
            }
        }

        private Bitmap DecodeStream(Stream stream, Request<Bitmap> date)
        {
            BitmapFactory.Options options = CreateBitmapOptions(date);
            bool calculateSize = RequiresInSampleSize(options);

            byte[] bytes = Utils.ToByteArray(stream);
            if (calculateSize)
            {
                BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
                CalculateInSampleSize(date.TargetWidth, date.TargetHeight, options, date);
            }
            return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
        }
    }
}

