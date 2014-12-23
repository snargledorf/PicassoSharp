using System;
using System.IO;

namespace PicassoSharp
{
    public interface IDownloader<TBitmap>
    {
        Response<TBitmap> Load(Uri uri, bool localCacheOnly);

        void Shutdown();
    }

    public class ResponseException : IOException
    {
        public ResponseException(string message) 
            : base(message)
        {
            
        }
    }

    public class Response<TBitmap>
    {
        private readonly Stream m_Stream;
        private readonly bool m_Cached;
        private readonly TBitmap m_Bitmap;
        private readonly long m_ContentLength;

        public Response(TBitmap bitmap, bool cached, long contentLength)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("Bitmap may not be null.");
            }
            m_Stream = null;
            m_Bitmap = bitmap;
            m_Cached = cached;
            m_ContentLength = contentLength;
        }

        public Response(Stream stream, bool cached, long contentLength)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("Stream may not be null.");
            }
            m_Stream = stream;
            m_Bitmap = default(TBitmap);
            m_Cached = cached;
            m_ContentLength = contentLength;
        }

        public Stream Stream
        {
            get { return m_Stream; }
        }

        public bool Cached
        {
            get { return m_Cached; }
        }

        public TBitmap Bitmap 
        {
            get { return m_Bitmap; }
        }

        public long ContentLength
        {
            get { return m_ContentLength; }
        }
    }
}