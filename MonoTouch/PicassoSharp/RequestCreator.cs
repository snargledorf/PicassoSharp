using System;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public sealed class RequestCreator
    {
		readonly Request.Builder m_RequestBuilder;
		readonly Picasso m_Picasso;

		bool m_SkipCache = false;
		bool m_NoFade = false;

		UIImage m_PlaceholderImage;
        UIImage m_ErrorImage;

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

//		public RequestCreator NoFade()
//		{
//			m_NoFade = true;
//			return this;
//		}

//		public RequestCreator Fit(int width, int height)
//		{
//			m_RequestBuilder.Resize(width, height);
//			return this;
//		}

		public RequestCreator PlaceholderImage(UIImage placeholderImage)
		{
			m_PlaceholderImage = placeholderImage;
			return this;
		}

        public RequestCreator ErrorDrawable(UIImage errorImage)
		{
			m_ErrorImage = errorImage;
			return this;
		}

        public void Into(ITarget target)
        {
			if (target == null)
				throw new ArgumentNullException("target");

			if (m_OnStartListener != null)
			{
				m_OnStartListener ();
			}

            target.OnPrepareLoad(m_PlaceholderImage);

			Request request = m_RequestBuilder.Build();
			string key = Utils.CreateKey(request);

			Action action = new TargetAction(
				m_Picasso,
				target, 
				request,
                m_SkipCache,
                m_NoFade,
				key,
				m_ErrorImage,
                m_OnSuccessListener,
                m_OnFailureListener,
                m_OnFinishListener);

			if (!m_SkipCache)
            {
                UIImage cachedImage = m_Picasso.Cache.Get(key);
				if (cachedImage != null)
                {
                    m_Picasso.CancelRequest(target);
                    action.Complete(cachedImage, LoadedFrom.Memory);
                    return;
                }
            }

            m_Picasso.EnqueueAndSubmit(action);
        }

		public void Into(UIImageView target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

			if (m_OnStartListener != null)
			{
				m_OnStartListener ();
			}

            if (m_PlaceholderImage != null)
                target.Image = m_PlaceholderImage;

            Request request = m_RequestBuilder.Build();
            string key = Utils.CreateKey(request);

            var action = new UIImageViewAction(
                m_Picasso,
                target,
                request,
                m_SkipCache,
                m_NoFade,
                key,
                m_ErrorImage,
                m_OnSuccessListener,
                m_OnFailureListener,
                m_OnFinishListener);

            if (!m_SkipCache)
            {
                UIImage cachedImage = m_Picasso.Cache.Get(key);
                if (cachedImage != null)
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

