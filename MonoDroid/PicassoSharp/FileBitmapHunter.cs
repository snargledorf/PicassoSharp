using System;
using System.Collections.Generic;
using System.IO;
using Android.Graphics;
using Android.Media;
using Stream = System.IO.Stream;

namespace PicassoSharp
{
	class FileBitmapHunter : BitmapHunter
	{
		internal FileBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache) 
			: base(picasso, action, dispatcher, cache)
		{
		}

		protected override Bitmap Decode(Request data)
		{
			LoadedFrom = LoadedFrom.Disk;

		    ExifRotation = GetFileExifRotation(data.Uri);

			Stream imageStream = File.OpenRead(data.Uri.AbsolutePath);

			return DecodeStream(imageStream);
		}

	    private static int GetFileExifRotation(Uri uri)
	    {
            var exifInterface = new ExifInterface(uri.AbsolutePath);
	        var orientation = (Orientation)exifInterface.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
	        switch (orientation)
	        {
                case Orientation.Rotate90:
	                return 90;
                case Orientation.Rotate180:
	                return 180;
                case Orientation.Rotate270:
	                return 270;
                default:
	                return 0;
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
                CalculateInSampleSize(Data.TargetWidth, Data.TargetHeight, options, Data);
            }
            return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
        }
	}

}

