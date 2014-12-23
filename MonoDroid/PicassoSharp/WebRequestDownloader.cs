using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using Android.Graphics;

namespace PicassoSharp
{
    class WebRequestDownloader : IDownloader<Bitmap>
    {
        public Response<Bitmap> Load(Uri uri, bool localCacheOnly)
        {
            var request = new HttpWebRequest(uri);

            // Currently Xamarin doesn't support WebRequest caching
            // This is here so that once they enable it we are all ready set up
            if (localCacheOnly)
            {
                var cachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.CacheOnly);
                request.CachePolicy = cachePolicy;
            }
            else
            {
                var cachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.CacheIfAvailable);
                request.CachePolicy = cachePolicy;
            }

            var responce = request.GetResponse() as HttpWebResponse;
            if (responce == null)
            {
                throw new ResponseException("Response came back as null");
            }

            if (responce.StatusCode >= HttpStatusCode.Ambiguous)
            {
                request.Abort();
                throw new ResponseException(responce.StatusCode + " " + responce.StatusDescription);
            }

            Stream stream = responce.GetResponseStream();
            long contentLength = responce.ContentLength;
            bool cached = responce.IsFromCache;

            return new Response<Bitmap>(stream, cached, contentLength);
        }

        public void Shutdown()
        {
            // No Op
        }
    }
}