using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.Graphics;
using Android.Graphics.Drawables;
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
	class BitmapHunter : Object, IBitmapHunter<Bitmap, Drawable>, IRunnable
    {
        /*
        * Global lock for bitmap decoding to ensure that we are only are decoding one at a time. Since
        * this will only ever happen in background threads we help avoid excessive memory thrashing as
        * well as potential OOMs. Shamelessly stolen from Volley.
        */
        private static readonly object s_DecodeLock = new object();

        private static readonly ThreadLocal<StringBuilder> s_NameBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(Utils.ThreadPrefix));
        private static readonly ErrorRequestHandler<Bitmap> s_ErrorHandler = new ErrorRequestHandler<Bitmap>();

        private readonly IPicasso<Bitmap, Drawable> m_Picasso;
		private readonly Dispatcher m_Dispatcher;
		private readonly ICache<Bitmap> m_Cache;
        private readonly IRequestHandler<Bitmap> m_RequestHandler;
	    private readonly bool m_SkipCache;
		private readonly Request<Bitmap> m_Data;
		private readonly string m_Key;

        protected BitmapHunter(IPicasso<Bitmap, Drawable> picasso, Action<Bitmap, Drawable> action, Dispatcher dispatcher, ICache<Bitmap> cache, IRequestHandler<Bitmap> requestHandler)
        {
            Action = action;
			m_Data = action.Request;
			m_Key = action.Key;
			m_Picasso = picasso;
            m_Dispatcher = dispatcher;
			m_Cache = cache;
            m_RequestHandler = requestHandler;
            m_SkipCache = action.SkipCache;
        }

        public Action<Bitmap, Drawable> Action { get; private set; }

        public List<Action<Bitmap, Drawable>> Actions { get; private set; }

        public IPicasso<Bitmap, Drawable> Picasso
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

        public Request<Bitmap> Data
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

	    public int ExifRotation
	    {
	        get; 
            protected set; 
        }

	    public IFuture Future { get; set; }

	    public bool Cancelled 
        {
	        get { return Future != null && Future.IsCancelled; }
        }

        public void Attach(Action<Bitmap, Drawable> action)
        {
            if (Action == null)
            {
                Action = action;
                return;
            }

            if (Actions == null)
            {
                Actions = new List<Action<Bitmap, Drawable>>();
            }
            Actions.Add(action);
        }

        public void Detach(Action<Bitmap, Drawable> action)
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
        
		internal Bitmap Hunt()
		{
		    Bitmap bitmap = null;

			if (!m_SkipCache)
			{
				bitmap = m_Cache.Get(Key);
				if (bitmap != null)
				{
					LoadedFrom = LoadedFrom.Memory;
					return bitmap;
				}
			}

            Result<Bitmap> result = m_RequestHandler.Load(Data);
		    if (result != null)
		    {
		        bitmap = result.Bitmap;
		        LoadedFrom = result.LoadedFrom;
		        ExifRotation = result.ExifOrientation;
		    }

		    if (bitmap != null)
		    {
		        if (Data.NeedsTransformation || ExifRotation != 0)
		        {
		            lock (s_DecodeLock)
                    {
                        if (Data.NeedsMatrixTransform || ExifRotation != 0)
                        {
                            bitmap = TransformResult(Data, bitmap, ExifRotation);
                        }
                        if (Data.HasCustomTransformations)
                        {
                            bitmap = ApplyCustomTransformations(Data.Transformations, bitmap);
                        }
		            }
		        }
		    }

			return bitmap;
		}

	    private void UpdateThreadName(Request<Bitmap> data)
        {
            string name = data.Name;

            StringBuilder builder = s_NameBuilder.Value;
            builder.EnsureCapacity(Utils.ThreadPrefix.Length + name.Length);
            builder.Insert(Utils.ThreadPrefix.Length, name);

            Thread.CurrentThread().Name = builder.ToString();
        }

        public static BitmapHunter ForRequest(IPicasso<Bitmap, Drawable> picasso, Action<Bitmap, Drawable> action, Dispatcher dispatcher, ICache<Bitmap> cache)
		{
            Request<Bitmap> request = action.Request;
            IList<IRequestHandler<Bitmap>> requestHandlers = picasso.RequestHandlers;

            // Index-based loop to avoid allocating an iterator.
            for (int i = 0, count = requestHandlers.Count; i < count; i++)
            {
                var requestHandler = requestHandlers[i] as RequestHandler;
                if (requestHandler != null && requestHandler.CanHandleRequest(request))
                {
                    return new BitmapHunter(picasso, action, dispatcher, cache, requestHandler);
                }
            }

            return new BitmapHunter(picasso, action, dispatcher, cache, s_ErrorHandler);
		}

	    public virtual bool ShouldRetry(bool airplaneMode, NetworkInfo info)
	    {
            return false;
	    }

        private Bitmap ApplyCustomTransformations(List<ITransformation<Bitmap>> transformations, Bitmap result)
        {
            if (result == null) throw new ArgumentNullException("result");

            for (int i = 0; i < transformations.Count; i++)
            {
                ITransformation<Bitmap> transformation = transformations[i];
                Bitmap newResult = transformation.Transform(result);

                if (newResult == null)
                {
                    StringBuilder builder = new StringBuilder()
                        .Append("Transformation ")
                        .Append(transformation.Key)
                        .Append(" returned null after ")
                        .Append(i)
                        .Append(" previous transformation(s).\n\nTransformation list:\n");
                    foreach (ITransformation<Bitmap> t in transformations)
                    {
                        builder.Append(t.Key).Append('\n');
                    }
                    Picasso.RunOnPicassoThread(() =>
                    {
                        throw new NullReferenceException(builder.ToString());
                    });
                    return null;
                }

                if (newResult == result && result.IsRecycled)
                {
                    Picasso.RunOnPicassoThread(() =>
                    {
                        throw new IllegalStateException("Transformation " +
                                                        transformation.Key + " return input Bitmap but recycled it.");
                    });
                    return null;
                }

                // If the transformation returned a new bitmap ensure they recycled the original.
                if (newResult != result && !result.IsRecycled)
                {
                    Picasso.RunOnPicassoThread(() =>
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

        static Bitmap TransformResult(Request<Bitmap> data, Bitmap result, int exifRotation)
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

                float targetRotation = data.RotationDegrees;
                if (targetRotation != 0)
                {
                    if (data.HasRotationPivot)
                    {
                        matrix.SetRotate(targetRotation, data.RotationPivotX, data.RotationPivotY);
                    }
                    else
                    {
                        matrix.SetRotate(targetRotation);
                    }
                }

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

            if (exifRotation != 0)
            {
                matrix.PreRotate(exifRotation);
            }

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

