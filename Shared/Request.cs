using System;
using System.Collections.Generic;
using Java.Lang;
using Exception = System.Exception;

namespace PicassoSharp
{
	public class Request
    {
        private Request(Uri uri, int resourceId, string stableKey, int targetWidth, int targetHeight, bool centerCrop, bool centerInside, List<ITransformation> transformations, float rotationDegrees, float rotationPivotX, float rotationPivotY, bool hasRotationPivot)
        {
	        Transformations = transformations;
            RotationDegrees = rotationDegrees;
            RotationPivotX = rotationPivotX;
            RotationPivotY = rotationPivotY;
            HasRotationPivot = hasRotationPivot;
            TargetWidth = targetWidth;
			TargetHeight = targetHeight;
	        CenterCrop = centerCrop;
	        CenterInside = centerInside;
	        Uri = uri;
	        ResourceId = resourceId;
            StableKey = stableKey;
        }

		public Uri Uri 
		{ 
			get;
			private set;
        }

	    public int ResourceId { get; private set; }
	    public string StableKey { get; private set; }

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

	    public bool CenterCrop { get; private set; }
	    public bool CenterInside { get; private set; }
        public float RotationDegrees { get; private set; }
	    public float RotationPivotX { get; private set; }
	    public float RotationPivotY { get; private set; }

	    public List<ITransformation> Transformations { get; private set; }

	    public string Name
	    {
	        get
	        {
	            if (Uri != null)
	                return Uri.AbsolutePath;
	            return Integer.ToHexString(ResourceId);
	        }
	    }

        public bool HasSize { get { return TargetWidth != 0 || TargetHeight != 0; } }
	    public bool NeedsTransformation { get { return NeedsMatrixTransform || HasCustomTransformations; } }
	    public bool NeedsMatrixTransform { get { return HasSize || RotationDegrees != 0; } }
        public bool HasCustomTransformations { get { return Transformations != null; } }
        public bool HasRotationPivot { get; private set; }

        public Builder BuildUpon()
        {
            return new Builder(this);
        }

	    public class Builder
        {
            private Uri m_Uri;
            private int m_ResourceId;
			private int m_TargetHeight;
			private int m_TargetWidth;
	        private List<ITransformation> m_Transformations;
	        private bool m_CenterCrop;
	        private bool m_CenterInside;
	        private float m_RotationDegrees;
	        private float m_RotationPivotX;
	        private float m_RotationPivotY;
	        private bool m_HasRotationPivot;
	        private string m_StableKey;

	        public Builder(int resourceId)
	        {
	            SetResourceId(resourceId);
	        }

	        public Builder(Uri uri)
	        {
	            SetUri(uri);
	        }

            public Builder(Uri uri, int resourceId)
            {
                m_Uri = uri;
	            m_ResourceId = resourceId;
	        }

	        public Builder(Request request)
	        {
	            m_Uri = request.Uri;
	            m_ResourceId = request.ResourceId;
                m_StableKey = request.StableKey;
                m_TargetWidth = request.TargetWidth;
                m_TargetHeight = request.TargetHeight;
                m_CenterCrop = request.CenterCrop;
                m_CenterInside = request.CenterInside;
                m_RotationDegrees = request.RotationDegrees;
                m_RotationPivotX = request.RotationPivotX;
                m_RotationPivotY = request.RotationPivotY;
                m_HasRotationPivot = request.HasRotationPivot;
                if (request.Transformations != null)
                {
                    m_Transformations = new List<ITransformation>(request.Transformations);
                }
	        }

	        public bool HasImage
	        {
	            get { return m_Uri != null || m_ResourceId != 0; }
	        }

	        public bool HasSize
	        {
	            get { return m_TargetHeight != 0 || m_TargetWidth != 0; }
	        }

	        public Builder SetUri(Uri uri)
	        {
	            if (uri == null)
	            {
	                throw new ArgumentNullException("uri");
	            }
	            m_Uri = uri;
	            m_ResourceId = 0;
	            return this;
	        }

	        public Builder SetResourceId(int resourceId)
	        {
	            if (resourceId == 0)
	            {
	                throw new ArgumentException("Image resource ID may not be 0.");
	            }
	            m_ResourceId = resourceId;
	            m_Uri = null;
	            return this;
	        }

	        public Builder StableKey(string stableKey)
	        {
	            m_StableKey = stableKey;
	            return this;
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

            public Builder Rotate(float degrees)
            {
                m_RotationDegrees = degrees;
                return this;
            }

            public Builder Rotate(float degrees, float pivotX, float pivotY)
            {
                m_RotationDegrees = degrees;
                m_RotationPivotX = pivotX;
                m_RotationPivotY = pivotY;
                m_HasRotationPivot = true;
                return this;
            }

	        public Builder ClearRotation()
	        {
	            m_RotationDegrees = 0;
	            m_RotationPivotX = 0;
	            m_RotationPivotY = 0;
	            m_HasRotationPivot = false;
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
			    return new Request(
                    m_Uri,
			        m_ResourceId,
                    m_StableKey,
			        m_TargetWidth,
			        m_TargetHeight,
			        m_CenterCrop,
			        m_CenterInside,
			        m_Transformations,
                    m_RotationDegrees,
                    m_RotationPivotX,
                    m_RotationPivotY,
                    m_HasRotationPivot);
            }
        }
    }
}

