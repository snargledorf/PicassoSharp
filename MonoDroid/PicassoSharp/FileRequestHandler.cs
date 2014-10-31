using System;
using System.Collections.Generic;
using System.IO;
using Android.Graphics;
using Android.Media;
using Stream = System.IO.Stream;

namespace PicassoSharp
{
	class FileRequestHandler : RequestHandler
	{
	    public override bool CanHandleRequest(Request data)
        {
            return data.Uri.IsFile;
        }

	    public override Result Load(Request data)
        {
            return new Result(DecodeStream(data), LoadedFrom.Disk, GetFileExifRotation(data.Uri));
        }

        private Bitmap DecodeStream(Request data)
        {
            var stream = File.OpenRead(data.Uri.AbsolutePath);
            BitmapFactory.Options options = CreateBitmapOptions(data);
            bool calculateSize = RequiresInSampleSize(options);

            byte[] bytes = Utils.ToByteArray(stream);
            if (calculateSize)
            {
                BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
                CalculateInSampleSize(data.TargetWidth, data.TargetHeight, options, data);
            }
            return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
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
    }

}

