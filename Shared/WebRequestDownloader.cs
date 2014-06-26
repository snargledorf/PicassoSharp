using System;
using System.IO;
using System.Net;
using System.Net.Cache;

namespace PicassoSharp
{
    class WebRequestDownloader : IDownloader
    {
        public WebRequestDownloader()
        {
        }

        public Response Load(Uri uri, bool localCacheOnly)
        {
            var request = WebRequest.Create(uri);

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

            WebResponse responce = request.GetResponse();

            Stream stream = responce.GetResponseStream();
            bool cached = responce.IsFromCache;

            return new Response(stream, cached);
        }
    }
}