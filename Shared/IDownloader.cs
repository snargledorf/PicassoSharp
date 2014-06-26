using System;
using System.IO;

namespace PicassoSharp
{
    public interface IDownloader
    {
        Response Load(Uri uri, bool localCacheOnly);
    }

    public class ResponseException : IOException
    {
        public ResponseException(string message) 
            : base(message)
        {
            
        }
    }

    public class Response
    {
        private readonly Stream m_BitmapStream;
        private readonly bool m_Cached;

        public Response(Stream bitmapStream, bool cached)
        {
            m_BitmapStream = bitmapStream;
            m_Cached = cached;
        }

        public Stream BitmapStream
        {
            get { return m_BitmapStream; }
        }

        public bool Cached
        {
            get { return m_Cached; }
        }
    }
}