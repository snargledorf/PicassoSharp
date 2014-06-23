using System;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PicassoSharp
{
	public abstract class Action : Java.Lang.Object
    {
		private readonly WeakReference<Object> m_Target;
		private readonly Picasso m_Picasso;
		private readonly Request m_Data;
		private readonly bool m_SkipCache;
		private readonly bool m_NoFade;
		private readonly string m_Key;
		private readonly Drawable m_ErrorDrawable;

		public Action(
			Picasso picasso, 
			Object target, 
			Request data, 
			bool skipCache, 
			bool noFade,
			string key, 
			Drawable errorDrawable)
        {
			m_Target = new WeakReference<object>(target);
			m_Picasso = picasso;
			m_Data = data;
			m_Key = key;
			m_SkipCache = skipCache;
			m_NoFade = noFade;
			m_ErrorDrawable = errorDrawable;
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

		public Drawable ErrorDrawable
		{
			get
			{
				return m_ErrorDrawable;
			}
		}

		public bool NoFade
		{
			get
			{
				return m_NoFade;
			}
		}

		public abstract void Complete(Bitmap bitmap, LoadedFrom loadedFrom);

		public abstract void Error();

        public void Cancel()
        {
            Cancelled = true;
        }
	}
}

