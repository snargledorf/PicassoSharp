using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	abstract class BitmapHunter : NSOperation
	{
	    private static readonly ThreadLocal<StringBuilder> NameBuilder = new ThreadLocal<StringBuilder>();
        
	    private readonly Picasso m_Picasso;
		private readonly Dispatcher m_Dispatcher;
		private readonly ICache<UIImage> m_Cache;
	    private readonly IDownloader m_Downloader;
	    private readonly bool m_SkipCache;
		private readonly Request m_Data;
		private readonly string m_Key;

	    protected BitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<UIImage> cache, IDownloader downloader)
        {
            Action = action;
			m_Data = action.Data;
			m_Key = action.Key;
			m_Picasso = picasso;
            m_Dispatcher = dispatcher;
			m_Cache = cache;
	        m_Downloader = downloader;
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

		public UIImage Result
        {
            get;
            private set;
        }

		Exception Exception
		{
			get;
			set;
		}

		public LoadedFrom LoadedFrom
		{
			get;
			protected set;
		}

	    public IDownloader Downloader
	    {
	        get { return m_Downloader; }
	    }

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

        public override void Cancel()
        {
            if (Action == null && (Actions == null || Actions.Count == 0))
            {
                base.Cancel();
            }
        }

        public override void Main()
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
                // TODO Dispatch retry
//            catch (IOException ex)
//            {
//                Exception = ex;
//                m_Dispatcher.DispatchRetry(this);
//            }
            catch (Exception ex)
            {
                Exception = ex;
                m_Dispatcher.DispatchFailed(this);
            }
            finally
            {
                Thread.CurrentThread.Name = Utils.ThreadIdleName;
            }
        }

	    protected abstract UIImage Decode(Request data);

        private UIImage Hunt()
		{
            UIImage bitmap;

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

        private void UpdateThreadName(Request data)
        {
            string name = data.Name;

            StringBuilder builder = NameBuilder.Value;
            builder.EnsureCapacity(Utils.ThreadPrefix.Length + name.Length);
            builder.Insert(Utils.ThreadPrefix.Length, name);

            Thread.CurrentThread.Name = builder.ToString();
        }

		public static BitmapHunter ForRequest(Picasso picasso, Action action, Dispatcher dispatcher, ICache<UIImage> cache, IDownloader downloader)
		{
		    if (action.Data.Uri.IsFile)
			{
				return new FileBitmapHunter(picasso, action, dispatcher, cache, downloader);
			}
		    return new NetworkBitmapHunter(picasso, action, dispatcher, cache, downloader);
		}
	}
}

