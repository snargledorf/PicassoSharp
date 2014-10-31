using System;
using Android.Graphics;
using Android.Net;

namespace PicassoSharp
{
    public abstract class RequestHandler
    {
        public virtual int RetryCount
        {
            get { return 0; }
        }

        public virtual bool SupportsReplay
        {
            get { return false; }
        }

        public abstract bool CanHandleRequest(Request data);

        public abstract Result Load(Request data);

        public virtual bool ShouldRetry(bool airplaneMode, NetworkInfo info)
        {
            return false;
        }

        protected static BitmapFactory.Options CreateBitmapOptions(Request data)
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

        protected static void CalculateInSampleSize(int targetWidth, int targetHeight, BitmapFactory.Options options, Request request)
        {
            CalculateInSampleSize(targetWidth, targetHeight, options.OutWidth, options.OutHeight, options, request);
        }

        protected static void CalculateInSampleSize(int reqWidth, int reqHeight, int width, int height,
            BitmapFactory.Options options, Request request)
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

        public class Result
        {
            private readonly LoadedFrom m_LoadedFrom;
            private readonly Bitmap m_Bitmap;
            private readonly int m_ExifOrientation;

            public Bitmap Bitmap
            {
                get { return m_Bitmap; }
            }

            public LoadedFrom LoadedFrom
            {
                get { return m_LoadedFrom; }
            }

            internal int ExifOrientation
            {
                get { return m_ExifOrientation; }
            }

            public Result(Bitmap bitmap, LoadedFrom loadedFrom) : this(bitmap, loadedFrom, 0) { }

            public Result(Bitmap bitmap, LoadedFrom loadedFrom, int exifOrientation)
            {
                m_Bitmap = bitmap;
                m_LoadedFrom = loadedFrom;
                m_ExifOrientation = exifOrientation;
            }
        }
    }
}