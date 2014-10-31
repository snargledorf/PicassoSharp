using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;

namespace PicassoSharp
{
	public sealed class RequestCreator
    {
        private readonly Request.Builder m_Data;
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

			m_Data = new Request.Builder(uri, resourceId);
			m_Picasso = picasso;
		}

		public RequestCreator SkipCache()
		{
			m_SkipCache = true;
			return this;
		}

	    public RequestCreator Transform(ITransformation transformation)
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

	    public void Into(ITarget target)
        {
			if (target == null)
				throw new ArgumentNullException("target");

            if (m_OnStartListener != null)
                m_OnStartListener();

            target.OnPrepareLoad(m_PlaceholderDrawable);

	        Request request = CreateRequest();
			string key = Utils.CreateKey(request);

			Action action = new TargetAction(
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

            m_Picasso.EnqueueAndSubmit(action);
        }

        public void Into(ImageView target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (m_OnStartListener != null)
                m_OnStartListener();
            
            if (m_Deferred)
            {
                int measuredWidth = target.MeasuredWidth;
                int measuredHeight = target.MeasuredHeight;
                if (measuredWidth == 0 || measuredHeight == 0)
                {
                    PicassoDrawable.SetPlaceholder(target, m_PlaceholderDrawable);
                    m_Picasso.Defer(target, new DeferredRequestCreator(this, target));
                    return;
                }
                m_Data.Resize(width, height);
            }

            Request request = CreateRequest();
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

        private Request CreateRequest()
        {
            return m_Picasso.TransformRequest(m_Data.Build());
        }
    }
}

