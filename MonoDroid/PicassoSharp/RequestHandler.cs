using System;
using Android.Graphics;
using Android.Net;

namespace PicassoSharp
{
    public abstract class RequestHandler : IRequestHandler<Bitmap>
    {
        public virtual int RetryCount
        {
            get { return 0; }
        }

        public virtual bool SupportsReplay
        {
            get { return false; }
        }

        public abstract bool CanHandleRequest(Request<Bitmap> data);

        public abstract Result<Bitmap> Load(Request<Bitmap> data);

        public virtual bool ShouldRetry(bool airplaneMode, NetworkInfo info)
        {
            return false;
        }

        protected static BitmapFactory.Options CreateBitmapOptions(Request<Bitmap> data)
        {
            bool justBounds = data.HasSize;
            BitmapFactory.Options options = null;
            if (justBounds)
            {
                options = new BitmapFactory.Options();
                options.InJustDecodeBounds = data.HasSize;
            }
            return options;
        }

        protected static bool RequiresInSampleSize(BitmapFactory.Options options)
        {
            return options != null && options.InJustDecodeBounds;
        }

        protected static void CalculateInSampleSize(int targetWidth, int targetHeight, BitmapFactory.Options options, Request<Bitmap> request)
        {
            CalculateInSampleSize(targetWidth, targetHeight, options.OutWidth, options.OutHeight, options, request);
        }

        protected static void CalculateInSampleSize(int reqWidth, int reqHeight, int width, int height,
            BitmapFactory.Options options, Request<Bitmap> request)
        {
            int sampleSize = 1;
            if (height > reqHeight || width > reqWidth)
            {
                if (reqHeight == 0)
                {
                    sampleSize = (int)Math.Floor((float)width / (float)reqWidth);
                }
                else if (reqWidth == 0)
                {
                    sampleSize = (int)Math.Floor((float)height / (float)reqHeight);
                }
                else
                {
                    int heightRatio = (int)Math.Floor((float)height / (float)reqHeight);
                    int widthRatio = (int)Math.Floor((float)width / (float)reqWidth);
                    sampleSize = request.CenterInside
                        ? Math.Max(heightRatio, widthRatio)
                        : Math.Min(heightRatio, widthRatio);
                }
            }
            options.InSampleSize = sampleSize;
            options.InJustDecodeBounds = false;
        }
    }
}