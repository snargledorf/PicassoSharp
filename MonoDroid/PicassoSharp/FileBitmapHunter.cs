using System;
using System.Collections.Generic;
using System.IO;
using Android.Graphics;

namespace PicassoSharp
{
	class FileBitmapHunter : BitmapHunter
	{
		internal FileBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache, IDownloader downloader) 
			: base(picasso, action, dispatcher, cache, downloader)
		{
		}

		protected override Bitmap Decode(Request data)
		{
			LoadedFrom = LoadedFrom.Disk;

			Stream imageStream = File.OpenRead(data.Uri.AbsolutePath);

			return DecodeStream(imageStream);
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

