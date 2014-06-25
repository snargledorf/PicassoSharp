using System;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public abstract class Action
    {
		private readonly WeakReference<Object> m_Target;
		private readonly Picasso m_Picasso;
		private readonly Request m_Data;
		private readonly bool m_SkipCache;
		private readonly bool m_NoFade;
		private readonly string m_Key;
        private readonly UIImage m_ErrorImage;
        private readonly System.Action m_OnSuccessListener;
        private readonly System.Action m_OnFailureListener;
        private readonly System.Action m_OnFinishListener;

		public Action(
			Picasso picasso, 
			Object target, 
			Request data, 
			bool skipCache, 
			bool noFade,
			string key,
            UIImage errorImage, 
            System.Action onSuccessListener,
            System.Action onFailureListener,
            System.Action onFinishListener)
        {
			m_Target = new WeakReference<object>(target);
			m_Picasso = picasso;
			m_Data = data;
			m_Key = key;
			m_SkipCache = skipCache;
			m_NoFade = noFade;
            m_ErrorImage = errorImage;
            m_OnSuccessListener = onSuccessListener;
            m_OnFailureListener = onFailureListener;
            m_OnFinishListener = onFinishListener;
        }

		public Picasso Picasso
		{
			get
			{
				return m_Picasso;
			}
		}

		public Object Target
        {
            get
			{
				Object target;
				m_Target.TryGetTarget(out target);
				return target;
            }
        }

        public Request Data
        {
            get
			{
				return m_Data;
			}
        }

        public string Key
        {
            get
			{
				return m_Key;
			}
        }

        public bool SkipCache
        {
            get
			{
				return m_SkipCache;
			}
        }

        public bool Cancelled
        {
            get;
            private set;
        }

		public UIImage ErrorImage
		{
			get
			{
				return m_ErrorImage;
			}
		}

		public bool NoFade
		{
			get
			{
				return m_NoFade;
			}
		}

        public void Complete(UIImage image, LoadedFrom loadedFrom)
        {
            OnComplete(image, loadedFrom);

            if (m_OnSuccessListener != null)
            {
                m_OnSuccessListener();
            }

            Finish();
        }

        protected abstract void OnComplete(UIImage image, LoadedFrom loadedFrom);

        public void Error()
        {
            OnError();

            if (m_OnFailureListener != null)
            {
                m_OnFailureListener();
            }

            Finish();
        }

        private void Finish()
        {
            if (m_OnFinishListener != null)
            {
                m_OnFinishListener();
            }
        }

        protected abstract void OnError();

        public void Cancel()
        {
            Cancelled = true;
        }
	}
}

