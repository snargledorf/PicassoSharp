using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;

namespace PicassoSharp
{
	public sealed class RequestCreator
    {
		readonly Request.Builder m_RequestBuilder;
		readonly Picasso m_Picasso;

		bool m_SkipCache = false;
		bool m_NoFade = false;

		Drawable m_PlaceholderDrawable;
		Drawable m_ErrorDrawable;
	    private System.Action m_OnStartListener;
	    private System.Action m_OnFinishListener;
	    private System.Action m_OnFailureListener;
	    private System.Action m_OnSuccessListener;

	    internal RequestCreator(Picasso picasso, Uri uri)
        {
		    if (picasso.IsShutdown)
		    {
		        throw new Exception("Picasso instance shutdown. Cannot submit new requests");
		    }

			m_RequestBuilder = new Request.Builder(uri);
			m_Picasso = picasso;
		}

		public RequestCreator SkipCache()
		{
			m_SkipCache = true;
			return this;
		}

		public RequestCreator NoFade()
		{
			m_NoFade = true;
			return this;
		}

		public RequestCreator Fit(int width, int height)
		{
			m_RequestBuilder.Resize(width, height);
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

        public void Into(Target target)
        {
			if (target == null)
				throw new ArgumentNullException("target");

            if (m_OnStartListener != null)
                m_OnStartListener();

            target.OnPrepareLoad(m_PlaceholderDrawable);

			Request request = m_RequestBuilder.Build();
			string key = Utils.CreateKey(request);

			Action action = new TargetAction(
				m_Picasso,
				target, 
				request,
                m_SkipCache,
                m_NoFade,
				key,
				m_ErrorDrawable, 
                m_OnSuccessListener, 
                m_OnFailureListener,
                m_OnFinishListener);

			if (!m_SkipCache)
            {
                Bitmap cachedImage = m_Picasso.Cache.Get(key);
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

            PicassoDrawable.SetPlaceholder(target, m_PlaceholderDrawable);

            Request request = m_RequestBuilder.Build();
            string key = Utils.CreateKey(request);

            var action = new ImageViewAction(
                m_Picasso,
                target,
                request,
                m_SkipCache,
                m_NoFade,
                key,
                m_ErrorDrawable,
                m_OnSuccessListener,
                m_OnFailureListener,
                m_OnFinishListener);

            if (!m_SkipCache)
            {
                Bitmap cachedImage = m_Picasso.Cache.Get(key);
                if (cachedImage != null && !cachedImage.IsRecycled)
                {
                    m_Picasso.CancelRequest(target);
                    action.Complete(cachedImage, LoadedFrom.Memory);
                    return;
                }
            }

            m_Picasso.EnqueueAndSubmit(action);
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
    }
}

