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

		internal RequestCreator(Picasso picasso, Uri uri)
        {
		    if (picasso.IsShutdown)
		    {
		        throw new Exception("Picasso instance shutdown. Cannot submit new requests");
		    }

			m_RequestBuilder = new Request.Builder(uri);
			m_Picasso = picasso;
		}

		public RequestCreator SkipCache(bool skipCache)
		{
			m_SkipCache = skipCache;
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

        public void Into(ITarget target)
        {
			if (target == null)
				throw new ArgumentNullException("target");

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
				m_ErrorDrawable);

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
	        Into(target, null);
	    }

        public void Into(ImageView target, ICallback callback)
        {
            if (target == null)
                throw new ArgumentNullException("target");

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
                callback);

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
    }
}

