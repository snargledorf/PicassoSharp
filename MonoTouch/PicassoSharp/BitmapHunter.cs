using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
    class BitmapHunter : IBitmapHunter<UIImage, UIImage>
    {
	    private static readonly ThreadLocal<StringBuilder> s_NameBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(Utils.ThreadPrefix));

        private readonly IPicasso<UIImage, UIImage> m_Picasso;
		private readonly Dispatcher m_Dispatcher;
		private readonly ICache<UIImage> m_Cache;
        private readonly IRequestHandler<UIImage> m_RequestHandler;
        private readonly bool m_SkipCache;
		private readonly Request<UIImage> m_Data;
		private readonly string m_Key;
	    private readonly CancellationTokenSource m_CancellationSource;
        private Task m_Task;
        private static readonly ErrorRequestHandler<UIImage> s_ErrorHandler = new ErrorRequestHandler<UIImage>();
        
        public static BitmapHunter ForRequest(IPicasso<UIImage, UIImage> picasso, Action<UIImage, UIImage> action, Dispatcher dispatcher, ICache<UIImage> cache)
        {
            Request<UIImage> request = action.Request;
            IList<IRequestHandler<UIImage>> requestHandlers = picasso.RequestHandlers;

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

	    private static UIImage TransformResult(Request<UIImage> data, UIImage result)
	    {
	        CGImage cgImage = result.CGImage;
	        int inWidth = cgImage.Width;
	        int inHeight = cgImage.Height;

	        int drawWidth = inWidth;
	        int drawHeight = inHeight;

	        SizeF newSize = result.Size;
	        float scale = result.CurrentScale;

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
	                float widthRatio = targetWidth/(float) inWidth;
	                float heightRatio = targetHeight/(float) inHeight;
	                if (widthRatio > heightRatio)
	                {
	                    scale = widthRatio;
	                    drawHeight = (int) Math.Ceiling(inHeight*(heightRatio/widthRatio));
	                }
	                else
	                {
	                    scale = heightRatio;
	                    drawWidth = (int) Math.Ceiling(inWidth*(widthRatio/heightRatio));
	                }

	                newSize = new SizeF(drawWidth, drawHeight);
	            }
	            else if (data.CenterInside)
	            {
	                float widthRatio = targetWidth/(float) inWidth;
	                float heightRatio = targetHeight/(float) inHeight;
	                scale = widthRatio < heightRatio ? widthRatio : heightRatio;
	                newSize = new SizeF(drawWidth, drawHeight);
	            }
	            else if (targetWidth != 0 && targetHeight != 0 && (targetWidth != inWidth || targetHeight != inHeight))
	            {
	                // If an explicit target size has been specified and they do not match the results bounds,
	                // resize the image appropriately.
	                newSize = new SizeF(targetWidth, targetHeight);
	            }
	        }

	        //            if (exifRotation != 0)
	        //            {
	        //                matrix.preRotate(exifRotation);
	        //            }

	        UIImage newResult = result.Scale(newSize, scale);
	        if (newResult != result)
	        {
	            result.Dispose();
	            result = newResult;
	        }

	        return result;
	    }

        private BitmapHunter(IPicasso<UIImage, UIImage> picasso, Action<UIImage, UIImage> action, Dispatcher dispatcher, ICache<UIImage> cache, IRequestHandler<UIImage> requestHandler)
        {
            Action = action;
			m_Data = action.Request;
			m_Key = action.Key;
			m_Picasso = picasso;
            m_Dispatcher = dispatcher;
			m_Cache = cache;
            m_RequestHandler = requestHandler;
            m_SkipCache = action.SkipCache;
	        m_CancellationSource = new CancellationTokenSource();
        }

        public Action<UIImage, UIImage> Action { get; private set; }

        public List<Action<UIImage, UIImage>> Actions { get; private set; }

        public IPicasso<UIImage, UIImage> Picasso
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

		public Request<UIImage> Data
		{
			get
			{
				return m_Data;
			}
		}

		public UIImage Result
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

        public void Attach(Action<UIImage, UIImage> action)
        {
            if (Action == null)
            {
                Action = action;
                return;
            }

            if (Actions == null)
            {
                Actions = new List<Action<UIImage, UIImage>>();
            }
            Actions.Add(action);
        }

        public void Detach(Action<UIImage, UIImage> action)
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

        public void Cancel()
        {
            if (Action == null && (Actions == null || Actions.Count == 0))
            {
                m_CancellationSource.Cancel();
            }
        }

        public bool IsCancelled { get { return m_Task != null && m_Task.IsCanceled; } }

        public void Run()
        {
            m_Task = Task.Factory.StartNew(() =>
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
                        if (!SkipCache)
                        {
                            m_Cache.Set(Key, Result);
                        }

                        m_Dispatcher.DispatchComplete(this);
                    }
                }
                catch (ResponseException ex)
                {
                    Exception = ex;
                    m_Dispatcher.DispatchFailed(this);
                }
                    // TODO Dispatch retry
                    //            catch (IOException ex)
                    //            {
                    //                Exception = ex;
                    //                m_Dispatcher.DispatchRetry(this);
                    //            }
                catch (OperationCanceledException)
                {
                    // NoOp
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    m_Dispatcher.DispatchFailed(this);
                }
                finally
                {
					NSThread.Current.Name = Utils.ThreadIdleName;
                }
            }, m_CancellationSource.Token);
        }
        
        private UIImage Hunt()
		{
            UIImage bitmap = null;

			if (!m_SkipCache)
			{
				bitmap = m_Cache.Get(Key);
				if (bitmap != null)
				{
					LoadedFrom = LoadedFrom.Memory;
					return bitmap;
				}
			}

            Result<UIImage> result = m_RequestHandler.Load(Data);
            if (result != null)
            {
                bitmap = result.Bitmap;
                LoadedFrom = result.LoadedFrom;
            }

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

        private void UpdateThreadName(Request<UIImage> data)
        {
            string name = data.Name;

            StringBuilder builder = s_NameBuilder.Value;
            builder.EnsureCapacity(Utils.ThreadPrefix.Length + name.Length);
            builder.Insert(Utils.ThreadPrefix.Length, name);

			NSThread.Current.Name = builder.ToString();
        }

        private UIImage ApplyCustomTransformations(List<ITransformation<UIImage>> transformations, UIImage result)
        {
            if (result == null) throw new ArgumentNullException("result");

            for (int i = 0; i < transformations.Count; i++)
            {
                ITransformation<UIImage> transformation = transformations[i];
                UIImage newResult = transformation.Transform(result);

                if (newResult == null)
                {
                    StringBuilder builder = new StringBuilder()
                        .Append("Transformation ")
                        .Append(transformation.Key)
                        .Append(" returned null after ")
                        .Append(i)
                        .Append(" previous transformation(message).\n\nTransformation list:\n");
                    foreach (ITransformation<UIImage> t in transformations)
                    {
                        builder.Append(t.Key).Append('\n');
                    }

                    // TODO Need a better way to invoke on main thread
                    new NSObject().InvokeOnMainThread(() =>
                    {
                        throw new NullReferenceException(builder.ToString());
                    });

                    return null;
                }

                // TODO Check if result is disposed
                if (newResult == result)
                {
                    m_Picasso.RunOnPicassoThread(() =>
                    {
                        throw new InvalidOperationException("Transformation " +
                                                        transformation.Key + " return input Bitmap but recycled it.");
                    });
                    return null;
                }

                // If the transformation returned a new bitmap ensure they recycled the original.
                // TODO Check if result is NOT disposed
                if (newResult != result)
                {
                    m_Picasso.RunOnPicassoThread(() =>
                    {
                        throw new InvalidOperationException("Transformation "
                                                        + transformation.Key
                                                        + " mutated input Bitmap but failed to recycle the original.");
                    });
                    return null;
                }

                result = newResult;
            }

            return result;
        }
    }
}

