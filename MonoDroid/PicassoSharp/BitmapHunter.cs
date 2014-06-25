using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.Graphics;
using Android.Net;
using Java.Lang;
using Java.Util.Concurrent;
using Exception = System.Exception;
using Math = System.Math;
using StringBuilder = System.Text.StringBuilder;
using Thread = Java.Lang.Thread;

namespace PicassoSharp
{
	abstract class BitmapHunter : Object, IRunnable
	{
        private static readonly ThreadLocal<StringBuilder> NameBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(Utils.ThreadPrefix));

	    private readonly Picasso m_Picasso;
		private readonly Dispatcher m_Dispatcher;
		private readonly ICache<Bitmap> m_Cache;
	    private readonly bool m_SkipCache;
		private readonly Request m_Data;
		private readonly string m_Key;

	    protected BitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache)
        {
            Action = action;
			m_Data = action.Data;
			m_Key = action.Key;
			m_Picasso = picasso;
            m_Dispatcher = dispatcher;
			m_Cache = cache;
	        m_SkipCache = action.SkipCache;
        }

	    public Action Action { get; private set; }

	    public List<Action> Actions { get; private set; }

	    public Picasso Picasso
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

		public Request Data
		{
			get
			{
				return m_Data;
			}
		}

		public Bitmap Result
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

	    public IFuture Future { get; set; }

	    public bool Cancelled 
        {
	        get { return Future != null && Future.IsCancelled; }
        }

        public virtual bool SupportsReplay { get { return false; } }

	    public void Attach(Action action)
        {
            if (Action == null)
            {
                Action = action;
                return;
            }

            if (Actions == null)
            {
                Actions = new List<Action>();
            }
            Actions.Add(action);
        }

        public void Detach(Action action)
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

        public bool Cancel()
		{
            return Action == null
            && (Actions == null || Actions.Count == 0)
			&& Future != null
            && Future.Cancel(false);
		}

        public void Run()
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
                    m_Dispatcher.DispatchComplete(this);
                }
            }
            catch (ResponseException ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchFailed(this);
            }
            catch (IOException ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchRetry(this);
            }
            catch (Exception ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchFailed(this);
            }
            finally
            {
                Thread.CurrentThread().Name = Utils.ThreadIdleName;
            }
        }

	    protected abstract Bitmap Decode(Request data);

		Bitmap Hunt()
		{
			Bitmap bitmap;

			if (!m_SkipCache)
			{
				bitmap = m_Cache.Get(Key);
				if (bitmap != null)
				{
					LoadedFrom = LoadedFrom.Memory;
					return bitmap;
				}
			}

			bitmap = Decode(Data);

            // TODO Transforms

			return bitmap;
		}

        protected static BitmapFactory.Options CreateBitmapOptions(Request data)
        {
            bool justBounds = data.HasSize;
            BitmapFactory.Options options = null;
            if (justBounds)
            {
                options = new BitmapFactory.Options();
                options.InJustDecodeBounds = data.HasSize;
            }
            return options;
        }

        protected static bool RequiresInSampleSize(BitmapFactory.Options options)
        {
            return options != null && options.InJustDecodeBounds;
        }

        protected static void CalculateInSampleSize(int targetWidth, int targetHeight, BitmapFactory.Options options)
        {
            CalculateInSampleSize(targetWidth, targetHeight, options.OutWidth, options.OutHeight, options);
        }

        protected static void CalculateInSampleSize(int reqWidth, int reqHeight, int width, int height,
            BitmapFactory.Options options)
        {
            int sampleSize = 1;
            if (height > reqHeight || width > reqWidth)
            {
                int heightRatio = (int)Math.Round((decimal)(height / reqHeight));
                int widthRatio = (int)Math.Round((decimal)(width / reqWidth));
                sampleSize = heightRatio < widthRatio ? heightRatio : widthRatio;
            }
            options.InSampleSize = sampleSize;
            options.InJustDecodeBounds = false;
        }

        private void UpdateThreadName(Request data)
        {
            string name = data.Name;

            StringBuilder builder = NameBuilder.Value;
            builder.EnsureCapacity(Utils.ThreadPrefix.Length + name.Length);
            builder.Insert(Utils.ThreadPrefix.Length, name);

            Thread.CurrentThread().Name = builder.ToString();
        }

        public static BitmapHunter ForRequest(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache, IDownloader downloader)
		{
		    if (action.Data.Uri.IsFile)
			{
				return new FileBitmapHunter(picasso, action, dispatcher, cache);
			}
		    return new NetworkBitmapHunter(picasso, action, dispatcher, cache, downloader);
		}

	    public virtual bool ShouldRetry(bool airplaneMode, NetworkInfo info)
	    {
            return false;
	    }
	}
}

