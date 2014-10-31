using System;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PicassoSharp
{
	public abstract class Action : Java.Lang.Object
    {
        private readonly WeakReference<Object> m_Target;
		private readonly Picasso m_Picasso;
		private readonly Request m_Request;
		private readonly bool m_SkipCache;
		private readonly string m_Key;
	    private readonly FadeMode m_FadeMode;
		private readonly Drawable m_ErrorDrawable;
	    private readonly System.Action m_OnSuccessListener;
	    private readonly System.Action m_OnFailureListener;
	    private readonly System.Action m_OnFinishListener;

	    protected Action(Picasso picasso, object target, Request request, bool skipCache, FadeMode fadeMode, string key, Drawable errorDrawable, System.Action onSuccessListener, System.Action onFailureListener, System.Action onFinishListener)
        {
            m_Target = target == null ? null : new WeakReference<Object>(target);
			m_Picasso = picasso;
			m_Request = request;
			m_Key = key;
			m_SkipCache = skipCache;
			m_ErrorDrawable = errorDrawable;
	        m_FadeMode = fadeMode;
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
                Object value;
                m_Target.TryGetTarget(out value);
                return value;
            }
        }

        public Request Request
        {
            get
			{
				return m_Request;
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

		public Drawable ErrorDrawable
		{
			get
			{
				return m_ErrorDrawable;
			}
		}

		public FadeMode FadeMode
		{
			get
			{
				return m_FadeMode;
			}
		}

	    protected System.Action OnFinishListener
	    {
	        get { return m_OnFinishListener; }
	    }

	    public void Complete(Bitmap bitmap, LoadedFrom loadedFrom)
	    {
	        OnComplete(bitmap, loadedFrom);

	        if (m_OnSuccessListener != null)
	        {
	            m_OnSuccessListener();
	        }

	        Finish();
	    }

	    protected abstract void OnComplete(Bitmap bitmap, LoadedFrom loadedFrom);

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

