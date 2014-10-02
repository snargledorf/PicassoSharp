using System;
using System.Collections.Generic;
using Exception = System.Exception;

namespace PicassoSharp
{
	public class Request
    {
	    private Request(Uri uri, int targetWidth, int targetHeight, bool centerCrop, bool centerInside, List<ITransformation> transformations)
        {
	        Transformations = transformations;
	        TargetWidth = targetWidth;
			TargetHeight = targetHeight;
	        CenterCrop = centerCrop;
	        CenterInside = centerInside;
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

	    public bool CenterCrop { get; set; }
	    public bool CenterInside { get; set; }

	    public List<ITransformation> Transformations { get; private set; }

	    public string Name
	    {
	        get { return Uri.AbsolutePath; }
	    }

	    public bool HasSize { get { return TargetWidth != 0; } }
	    public bool NeedsTransformation { get { return NeedsMatrixTransform || HasCustomTransformations; } }
	    public bool NeedsMatrixTransform { get { return TargetWidth != 0; } }
	    public bool HasCustomTransformations { get { return Transformations != null; } }

	    public class Builder
        {
			private readonly Uri m_Uri;
			private int m_TargetHeight;
			private int m_TargetWidth;
	        private List<ITransformation> m_Transformations;
	        private bool m_CenterCrop;
	        private bool m_CenterInside;

	        public Builder(Uri uri)
            {
				m_Uri = uri;
            }

			public Builder Resize(int targetWidth, int targetHeight)
            {
                if (targetWidth <= 0 || targetHeight <= 0)
                    throw new ArgumentException("targetWidth and targetHeight must be greater than 0: targetWidth=" + targetWidth + " targetHeight=" + targetHeight);
				m_TargetWidth = targetWidth;
				m_TargetHeight = targetHeight;
				return this;
            }

	        public Builder ClearResize()
	        {
	            m_TargetWidth = 0;
	            m_TargetHeight = 0;
                m_CenterCrop = false;
	            m_CenterInside = false;
                return this;
	        }

            public Builder Tranform(ITransformation transformation)
            {
                if (transformation == null)
                    throw new ArgumentNullException("transformation");

                if (m_Transformations == null)
                    m_Transformations = new List<ITransformation>();

                m_Transformations.Add(transformation);

                return this;
            }

            public Builder CenterCrop()
            {
                if (m_CenterInside)
                    throw new Exception("Cannot call CenterCrop after CenterInside");
                m_CenterCrop = true;

                return this;
            }

            public Builder ClearCeneterCrop()
            {
                m_CenterCrop = false;
                return this;
            }

            public Builder CenterInside()
            {
                if (m_CenterCrop)
                    throw new Exception("Cannot call CenterInside after CenterCrop");
                m_CenterInside = true;

                return this;
            }

	        public Builder ClearCenterInside()
	        {
	            m_CenterInside = false;
                return this;
	        }

			public Request Build()
            {
                if (m_CenterInside && m_CenterCrop)
                {
                    throw new NotSupportedException("Center crop and center inside can not be used together.");
                }
                if (m_CenterCrop && m_TargetWidth == 0)
                {
					throw new NotSupportedException("Center crop requires calling resize.");
                }
                if (m_CenterInside && m_TargetWidth == 0)
                {
					throw new NotSupportedException("Center inside requires calling resize.");
                }
				return new Request(m_Uri, 
				                   m_TargetWidth, 
				                   m_TargetHeight,
                                   m_CenterCrop,
                                   m_CenterInside,
                                   m_Transformations);
            }
        }
    }
}

