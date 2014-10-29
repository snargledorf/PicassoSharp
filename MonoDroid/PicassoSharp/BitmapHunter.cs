using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.Graphics;
using Android.Net;
using Java.Lang;
using Java.Util.Concurrent;
using Exception = System.Exception;
using Math = System.Math;
using Object = Java.Lang.Object;
using StringBuilder = System.Text.StringBuilder;
using Thread = Java.Lang.Thread;

namespace PicassoSharp
{
	abstract class BitmapHunter : Object, IRunnable
	{
        private static readonly ThreadLocal<StringBuilder> NameBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(Utils.ThreadPrefix));

	    private readonly Picasso m_Picasso;
		private readonly Dispatcher m_Dispatcher;
		private readonly ICache<Bitmap> m_Cache;
	    private readonly bool m_SkipCache;
		private readonly Request m_Data;
		private readonly string m_Key;

	    protected BitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache)
        {
            Action = action;
			m_Data = action.Data;
			m_Key = action.Key;
			m_Picasso = picasso;
            m_Dispatcher = dispatcher;
			m_Cache = cache;
	        m_SkipCache = action.SkipCache;
        }

	    public Action Action { get; private set; }

	    public List<Action> Actions { get; private set; }

	    public Picasso Picasso
		{
			get
			{
				return m_Picasso;
			}
		}

        public bool SkipCache
        {
            get 
            {
                return m_SkipCache;
            }
		}

		public string Key
		{
			get
			{
				return m_Key;
			}
		}

		public Request Data
		{
			get
			{
				return m_Data;
			}
		}

		public Bitmap Result
        {
            get;
            private set;
        }

		public Exception Exception
		{
			get;
			set;
		}

		public LoadedFrom LoadedFrom
		{
			get;
			protected set;
		}

	    public IFuture Future { get; set; }

	    public bool Cancelled 
        {
	        get { return Future != null && Future.IsCancelled; }
        }

	    public void Attach(Action action)
        {
            if (Action == null)
            {
                Action = action;
                return;
            }

            if (Actions == null)
            {
                Actions = new List<Action>();
            }
            Actions.Add(action);
        }

        public void Detach(Action action)
        {
            if (Action == action)
            {
                Action = null;
            }
            else if (Actions != null)
            {
                Actions.Remove(action);
            }
        }

        public bool Cancel()
		{
            return Action == null
            && (Actions == null || Actions.Count == 0)
			&& Future != null
            && Future.Cancel(false);
		}

        public void Run()
        {
            try
            {
                UpdateThreadName(m_Data);

                Result = Hunt();

                if (Result == null)
                {
                    m_Dispatcher.DispatchFailed(this);
                }
                else
                {
                    m_Dispatcher.DispatchComplete(this);
                }
            }
            catch (ResponseException ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchFailed(this);
            }
            catch (IOException ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchRetry(this);
            }
            catch (Exception ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchFailed(this);
            }
            finally
            {
                Thread.CurrentThread().Name = Utils.ThreadIdleName;
            }
        }

	    protected abstract Bitmap Decode(Request data);

		Bitmap Hunt()
		{
			Bitmap bitmap;

			if (!m_SkipCache)
			{
				bitmap = m_Cache.Get(Key);
				if (bitmap != null)
				{
					LoadedFrom = LoadedFrom.Memory;
					return bitmap;
				}
			}

			bitmap = Decode(Data);

		    if (bitmap != null)
		    {
		        if (Data.NeedsTransformation)
		        {
		            if (Data.NeedsMatrixTransform)
		            {
		                bitmap = TransformResult(Data, bitmap);
		            }
		            if (Data.HasCustomTransformations)
		            {
		                bitmap = ApplyCustomTransformations(Data.Transformations, bitmap);
		            }
		        }
		    }

			return bitmap;
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
	                sampleSize = (int) Math.Floor((float) width/(float) reqWidth);
	            }
	            else if (reqWidth == 0)
	            {
	                sampleSize = (int) Math.Floor((float) height/(float) reqHeight);
	            }
	            else
	            {
	                int heightRatio = (int) Math.Floor((float) height/(float) reqHeight);
	                int widthRatio = (int) Math.Floor((float) width/(float) reqWidth);
	                sampleSize = request.CenterInside
	                    ? Math.Max(heightRatio, widthRatio)
	                    : Math.Min(heightRatio, widthRatio);
	            }
	        }
	        options.InSampleSize = sampleSize;
	        options.InJustDecodeBounds = false;
	    }

        private void UpdateThreadName(Request data)
        {
            string name = data.Name;

            StringBuilder builder = NameBuilder.Value;
            builder.EnsureCapacity(Utils.ThreadPrefix.Length + name.Length);
            builder.Insert(Utils.ThreadPrefix.Length, name);

            Thread.CurrentThread().Name = builder.ToString();
        }

        public static BitmapHunter ForRequest(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache, IDownloader downloader)
		{
            if (action.Data.ResourceId > 0)
            {
                return new ResourceBitmapHunter(picasso, action, dispatcher, cache);
            }
		    if (action.Data.Uri.IsFile)
			{
				return new FileBitmapHunter(picasso, action, dispatcher, cache);
			}
		    return new NetworkBitmapHunter(picasso, action, dispatcher, cache, downloader);
		}

	    public virtual bool ShouldRetry(bool airplaneMode, NetworkInfo info)
	    {
            return false;
	    }

        private Bitmap ApplyCustomTransformations(List<ITransformation> transformations, Bitmap result)
        {
            if (result == null) throw new ArgumentNullException("result");

            for (int i = 0; i < transformations.Count; i++)
            {
                ITransformation transformation = transformations[i];
                Bitmap newResult = transformation.Transform(result);

                if (newResult == null)
                {
                    StringBuilder builder = new StringBuilder() //
                        .Append("Transformation ")
                        .Append(transformation.Key)
                        .Append(" returned null after ")
                        .Append(i)
                        .Append(" previous transformation(s).\n\nTransformation list:\n");
                    foreach (ITransformation t in transformations)
                    {
                        builder.Append(t.Key).Append('\n');
                    }
                    Picasso.Handler.Post(() => {
                                                   throw new NullReferenceException(builder.ToString());
                    });
                    return null;
                }

                if (newResult == result && result.IsRecycled)
                {
                    Picasso.Handler.Post(() =>
                    {
                        throw new IllegalStateException("Transformation " +
                                                        transformation.Key + " return input Bitmap but recycled it.");
                    });
                    return null;
                }

                // If the transformation returned a new bitmap ensure they recycled the original.
                if (newResult != result && !result.IsRecycled)
                {
                    Picasso.Handler.Post(() =>
                    {
                        throw new IllegalStateException("Transformation "
                                                        + transformation.Key
                                                        + " mutated input Bitmap but failed to recycle the original.");
                    });
                    return null;
                }

                result = newResult;
            }

            return result;
        }

        static Bitmap TransformResult(Request data, Bitmap result)
        {
            int inWidth = result.Width;
            int inHeight = result.Height;

            int drawX = 0;
            int drawY = 0;
            int drawWidth = inWidth;
            int drawHeight = inHeight;

            var matrix = new Matrix();

            if (data.NeedsMatrixTransform)
            {
                int targetWidth = data.TargetWidth;
                int targetHeight = data.TargetHeight;

//                float targetRotation = data.rotationDegrees;
//                if (targetRotation != 0)
//                {
//                    if (data.hasRotationPivot)
//                    {
//                        matrix.setRotate(targetRotation, data.rotationPivotX, data.rotationPivotY);
//                    }
//                    else
//                    {
//                        matrix.setRotate(targetRotation);
//                    }
//                }

                if (data.CenterCrop)
                {
                    float widthRatio = targetWidth / (float)inWidth;
                    float heightRatio = targetHeight / (float)inHeight;
                    float scale;
                    if (widthRatio > heightRatio)
                    {
                        scale = widthRatio;
                        int newSize = (int)Math.Ceiling(inHeight * (heightRatio / widthRatio));
                        drawY = (inHeight - newSize) / 2;
                        drawHeight = newSize;
                    }
                    else
                    {
                        scale = heightRatio;
                        int newSize = (int)Math.Ceiling(inWidth * (widthRatio / heightRatio));
                        drawX = (inWidth - newSize) / 2;
                        drawWidth = newSize;
                    }
                    matrix.PreScale(scale, scale);
                }
                else if (data.CenterInside)
                {
                    float widthRatio = targetWidth / (float)inWidth;
                    float heightRatio = targetHeight / (float)inHeight;
                    float scale = widthRatio < heightRatio ? widthRatio : heightRatio;
                    matrix.PreScale(scale, scale);
                }
                else if (targetWidth != 0 && targetHeight != 0 && (targetWidth != inWidth || targetHeight != inHeight))
                {
                    // If an explicit target size has been specified and they do not match the results bounds,
                    // pre-scale the existing matrix appropriately.
                    float sx = targetWidth / (float)inWidth;
                    float sy = targetHeight / (float)inHeight;
                    matrix.PreScale(sx, sy);
                }
            }

//            if (exifRotation != 0)
//            {
//                matrix.preRotate(exifRotation);
//            }

            Bitmap newResult =
                Bitmap.CreateBitmap(result, drawX, drawY, drawWidth, drawHeight, matrix, true);
            if (newResult != result)
            {
                result.Recycle();
                result = newResult;
            }

            return result;
        }
	}
}

