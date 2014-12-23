using System;
using Android.Content;
using Android.Graphics;
using Android.Media;

namespace PicassoSharp
{
    class FileRequestHandler : ContentStreamRequestHandler
	{
        internal FileRequestHandler(Context context) : base(context)
        {
        }

        public override bool CanHandleRequest(Request<Bitmap> data)
        {
            return data.Uri.IsFile;
        }

        public override Result<Bitmap> Load(Request<Bitmap> data)
        {
            return new Result<Bitmap>(DecodeContentStream(data), LoadedFrom.Disk, GetFileExifRotation(data.Uri));
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

