using System.IO;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
    internal class NetworkRequestHandler : RequestHandler
    {
        private readonly IDownloader<UIImage> m_Downloader;

        internal NetworkRequestHandler(IDownloader<UIImage> downloader)
        {
            m_Downloader = downloader;
        }
        
        public override bool CanHandleRequest(Request<UIImage> data)
        {
            string schema = data.Uri.Scheme;
            return System.Uri.UriSchemeHttp.Equals(schema) || System.Uri.UriSchemeHttps.Equals(schema);
        }

        public override Result<UIImage> Load(Request<UIImage> data)
        {
            Response<UIImage> response = m_Downloader.Load(data.Uri, false);
            if (response == null)
                return null;
            
            var loadedFrom = response.Cached ? LoadedFrom.Disk : LoadedFrom.Network;

            Stream stream = response.Stream;
            if (stream == null)
                return null;

            try
            {
                UIImage image = DecodeStream(stream);
                return new Result<UIImage>(image, loadedFrom);
            }
            finally
            {
                Utils.CloseQuietly(stream);
            }
        }

        private UIImage DecodeStream(Stream stream)
        {
            return UIImage.LoadFromData(NSData.FromStream(stream));
        }
    }
}

