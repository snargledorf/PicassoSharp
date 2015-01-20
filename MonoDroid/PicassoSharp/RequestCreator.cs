using System;
using System.Text;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;

namespace PicassoSharp
{
	public sealed class RequestCreator
    {
        private readonly Request<Bitmap>.Builder m_Data;
        private readonly Picasso m_Picasso;

        private bool m_SkipCache;
        private FadeMode m_FadeMode = PicassoSharp.FadeMode.NotFromMemory;
        private bool m_Deferred;

        private Drawable m_PlaceholderDrawable;
        private Drawable m_ErrorDrawable;

	    private System.Action m_OnStartListener;
	    private System.Action m_OnFinishListener;
	    private System.Action m_OnFailureListener;
	    private System.Action m_OnSuccessListener;

	    internal RequestCreator(Picasso picasso, Uri uri, int resourceId)
        {
		    if (picasso.IsShutdown)
		    {
		        throw new Exception("Picasso instance shutdown. Cannot submit new requests");
		    }

            m_Data = new Request<Bitmap>.Builder(uri, resourceId);
			m_Picasso = picasso;
		}

		public RequestCreator SkipCache()
		{
			m_SkipCache = true;
			return this;
		}

	    public RequestCreator Transform(ITransformation<Bitmap> transformation)
	    {
	        m_Data.Tranform(transformation);
            return this;
	    }

        public RequestCreator FadeMode(FadeMode mode)
		{
			m_FadeMode = mode;
			return this;
		}

        public RequestCreator Rotate(float degrees)
        {
            m_Data.Rotate(degrees);
            return this;
        }

        public RequestCreator Rotate(float degrees, float pivotX, float pivotY)
        {
            m_Data.Rotate(degrees, pivotX, pivotY);
            return this;
        }

        public RequestCreator Fit()
	    {
	        m_Deferred = true;
            return this;
	    }

        internal RequestCreator Unfit()
	    {
	        m_Deferred = false;
            return this;
	    }

        public RequestCreator Resize(int width, int height)
		{
			m_Data.Resize(width, height);
			return this;
		}

        public RequestCreator CenterCrop()
        {
            m_Data.CenterCrop();
            return this;
        }

        public RequestCreator CenterInside()
        {
            m_Data.CenterInside();
            return this;
        }

        public RequestCreator PlaceholderDrawable(Drawable placeholderDrawable)
		{
			m_PlaceholderDrawable = placeholderDrawable;
			return this;
		}

        public RequestCreator ErrorDrawable(Drawable errorDrawable)
		{
			m_ErrorDrawable = errorDrawable;
			return this;
		}

        public RequestCreator StableKey(string stableKey)
        {
            m_Data.StableKey(stableKey);
            return this;
        }

        public RequestCreator OnStartListener(System.Action action)
        {
            m_OnStartListener = action;
            return this;
        }

        public RequestCreator OnSuccessListener(System.Action action)
        {
            m_OnSuccessListener = action;
            return this;
        }

        public RequestCreator OnFailureListener(System.Action action)
        {
            m_OnFailureListener = action;
            return this;
        }

        public RequestCreator OnFinishListener(System.Action action)
        {
            m_OnFinishListener = action;
            return this;
        }

	    public Bitmap Get()
	    {
	        if (m_Deferred)
	        {
	            throw new InvalidOperationException("Fit cannot be used with get");
	        }

	        if (!m_Data.HasImage)
	        {
	            return null;
	        }

	        Request<Bitmap> request = CreateRequest();
	        string key = Utils.CreateKey(request, new StringBuilder());

            Action<Bitmap, Drawable> getAction = new GetAction<Bitmap, Drawable>(m_Picasso, request, m_SkipCache, key);
	        return BitmapHunter.ForRequest(m_Picasso, getAction, m_Picasso.Dispatcher, m_Picasso.Cache).Hunt();
	    }

	    public void Fetch()
	    {
	        if (m_Deferred)
	        {
                throw new InvalidOperationException("Fit cannot be used with fetch");
	        }

	        if (m_Data.HasImage)
	        {
	            Request<Bitmap> request = CreateRequest();
	            string key = Utils.CreateKey(request, new StringBuilder());

	            Action<Bitmap, Drawable> fetchAction = new FetchAction<Bitmap, Drawable>(m_Picasso, request, m_SkipCache, key);
	            m_Picasso.EnqueueAndSubmit(fetchAction);
	        }
	    }

	    public void Into(ITarget<Bitmap, Drawable, Drawable> target)
        {
			if (target == null)
				throw new ArgumentNullException("target");

            if (m_OnStartListener != null)
                m_OnStartListener();

            target.OnPrepareLoad(m_PlaceholderDrawable);

	        Request<Bitmap> request = CreateRequest();
			string key = Utils.CreateKey(request);

            Action<Bitmap, Drawable> action = new TargetAction<Bitmap, Drawable, Drawable>(
				m_Picasso,
				target, 
				request,
                m_SkipCache,
				key,
				m_ErrorDrawable, 
                m_OnSuccessListener, 
                m_OnFailureListener,
                m_OnFinishListener);

			if (!m_SkipCache)
			{
                Bitmap cachedImage = m_Picasso.QuickMemoryCacheCheck(key);
				if (cachedImage != null && !cachedImage.IsRecycled)
                {
                    m_Picasso.CancelRequest(target);
                    action.Complete(cachedImage, LoadedFrom.Memory);
                    return;
                }
            }

            m_Picasso.EnqueueAndSubmit(action);
        }

        public void Into(ImageView target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (!m_Data.HasImage)
            {
                m_Picasso.CancelRequest(target);
                return;
            }
            
            if (m_OnStartListener != null)
                m_OnStartListener();
            
            if (m_Deferred)
            {
                if (m_Data.HasSize)
                {
                    throw new InvalidOperationException("Fit cannot be used with resize.");
                }

                int width = target.Width;
                int height = target.Height;
                if (width == 0 || height == 0)
                {
                    PicassoDrawable.SetPlaceholder(target, m_PlaceholderDrawable);
                    m_Picasso.Defer(target, new DeferredRequestCreator(this, target));
                    return;
                }
                m_Data.Resize(width, height);
            }

            Request<Bitmap> request = CreateRequest();
            string key = Utils.CreateKey(request);

            var action = new ImageViewAction(
                m_Picasso,
                target,
                request,
                m_SkipCache,
                m_FadeMode,
                key,
                m_ErrorDrawable,
                m_OnSuccessListener,
                m_OnFailureListener,
                m_OnFinishListener);

            if (!m_SkipCache)
            {
                Bitmap cachedImage = m_Picasso.QuickMemoryCacheCheck(key);
                if (cachedImage != null && !cachedImage.IsRecycled)
                {
                    m_Picasso.CancelRequest(target);
                    action.Complete(cachedImage, LoadedFrom.Memory);
                    return;
                }
            }

            PicassoDrawable.SetPlaceholder(target, m_PlaceholderDrawable);

            m_Picasso.EnqueueAndSubmit(action);
        }

        private Request<Bitmap> CreateRequest()
        {
            return m_Picasso.TransformRequest(m_Data.Build());
        }
    }
}

