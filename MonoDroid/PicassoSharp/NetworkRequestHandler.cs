using System.IO;
using System.Runtime.Remoting.Messaging;
using Android.Graphics;

namespace PicassoSharp
{
    internal class NetworkRequestHandler : RequestHandler
    {
        private const int DefaultRetryCount = 2;

        private const string SchemaHttp = "http";
        private const string SchemaHttps = "https";
        
        private readonly IDownloader m_Downloader;
        private int m_RetryCount;

        public override int RetryCount
        {
            get { return DefaultRetryCount; }
        }

        internal NetworkRequestHandler(IDownloader downloader)
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

        public override bool CanHandleRequest(Request data)
        {
            string schema = data.Uri.Scheme;
            return SchemaHttp.Equals(schema) || SchemaHttps.Equals(schema);
        }

        public override Result Load(Request data)
        {
            Response response = m_Downloader.Load(data.Uri, false);
            if (response == null)
                return null;

            var loadedFrom = response.Cached ? LoadedFrom.Disk : LoadedFrom.Network;

            Bitmap bitmap = response.Bitmap;
            if (bitmap != null)
                return new Result(bitmap, loadedFrom);

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
                return new Result(DecodeStream(stream, data), loadedFrom);
            }
            finally
            {
                Utils.CloseQuietly(stream);
            }
        }

        private Bitmap DecodeStream(Stream stream, Request date)
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

