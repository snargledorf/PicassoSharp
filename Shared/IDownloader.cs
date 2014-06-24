using System;
using System.IO;

namespace PicassoSharp
{
    public interface IDownloader
    {
        Response Load(Uri uri);
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

        public Response(Stream bitmapStream)
        {
            m_BitmapStream = bitmapStream;
        }

        public Stream BitmapStream
        {
            get { return m_BitmapStream; }
        }
    }
}