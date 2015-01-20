using System;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public sealed class RequestCreator
    {
        private readonly Request<UIImage>.Builder m_RequestBuilder;
        private readonly Picasso m_Picasso;

        private bool m_SkipCache;

        private UIImage m_PlaceholderImage;
        private UIImage m_ErrorImage;

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

            m_RequestBuilder = new Request<UIImage>.Builder(uri);
			m_Picasso = picasso;
		}

		public RequestCreator SkipCache()
		{
            m_SkipCache = true;
			return this;
		}

        public RequestCreator CenterCrop()
        {
            m_RequestBuilder.CenterCrop();
            return this;
        }

        public RequestCreator CenterInside()
        {
            m_RequestBuilder.CenterInside();
            return this;
        }

        public RequestCreator Resize(int width, int height)
        {
            m_RequestBuilder.Resize(width, height);
            return this;
        }

		public RequestCreator PlaceholderImage(UIImage placeholderImage)
		{
			m_PlaceholderImage = placeholderImage;
			return this;
		}

        public RequestCreator ErrorImage(UIImage errorImage)
		{
			m_ErrorImage = errorImage;
			return this;
		}

        public void Into(ITarget<UIImage, UIImage, UIImage> target)
        {
			if (target == null)
				throw new ArgumentNullException("target");

			if (m_OnStartListener != null)
			{
				m_OnStartListener ();
			}

            target.OnPrepareLoad(m_PlaceholderImage);

			Request<UIImage> request = m_RequestBuilder.Build();
			string key = Utils.CreateKey(request);

			var action = new TargetAction<UIImage, UIImage, UIImage> (
				m_Picasso,
				target, 
				request,
                m_SkipCache,
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

            Request<UIImage> request = m_RequestBuilder.Build();
            string key = Utils.CreateKey(request);

            var action = new UIImageViewAction(
                m_Picasso,
                target,
                request,
                m_SkipCache, 
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

