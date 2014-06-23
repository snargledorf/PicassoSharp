using System.IO;
using Android.Graphics;

namespace PicassoSharp
{
    internal class NetworkBitmapHunter : BitmapHunter
    {
        internal NetworkBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache cache,
            IDownloader downloader)
            : base(picasso, action, dispatcher, cache, downloader)
        {
        }
        
        protected override Bitmap Decode(Request data)
        {
            LoadedFrom = LoadedFrom.Network;

            Response response = Downloader.Load(data.Uri);
            if (response == null)
                return null;

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
    }
}

