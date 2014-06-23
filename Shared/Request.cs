using System;

namespace PicassoSharp
{
	public class Request
    {
		private Request(Uri uri, 
		                int targetWidth, 
		                int targetHeight)
        {
			TargetWidth = targetWidth;
			TargetHeight = targetHeight;
			Uri = uri;
        }

		public Uri Uri 
		{ 
			get;
			private set;
        }

        public int TargetWidth
		{
			get;
			private set;
        }

        public int TargetHeight
		{
			get;
			private set;
        }

	    public string Name
	    {
	        get { return Uri.AbsolutePath; }
	    }

	    public bool HasSize { get { return TargetWidth != 0; } }

	    public class Builder
        {
			private Uri m_Uri;
			private int m_TargetHeight;
			private int m_TargetWidth;

			public Builder(Uri uri)
            {
				m_Uri = uri;
            }

			public Builder Resize(int targetWidth, int targetHeight)
            {
                if (targetWidth <= 0 || targetHeight <= 0)
                    throw new ArgumentException("targetWidth and targetHeight must be greater than 0: targetWidth = " + targetWidth + " targetHeight = " + targetHeight);
				m_TargetWidth = targetWidth;
				m_TargetHeight = targetHeight;
				return this;
            }

			public Request Build()
            {
				return new Request(m_Uri, 
				                   m_TargetWidth, 
				                   m_TargetHeight);
            }
        }
    }
}

