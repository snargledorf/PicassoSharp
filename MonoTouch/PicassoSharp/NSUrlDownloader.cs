using System;
using MonoTouch.Foundation;

namespace PicassoSharp
{
    class NSUrlDownloader : IDownloader
    {
        public Response Load(Uri uri, bool localCacheOnly)
        {
            NSUrl url = NSUrl.FromString(uri.AbsoluteUri);

            var cachePolicy = localCacheOnly
                ? NSUrlRequestCachePolicy.ReturnCacheDataDoNotLoad
                : NSUrlRequestCachePolicy.ReturnCacheDataElseLoad;

            var request = new NSUrlRequest(url, cachePolicy, 20);

            NSCachedUrlResponse cachedResponse = NSUrlCache.SharedCache.CachedResponseForRequest(request);
            if (cachedResponse != null)
            {
                return new Response(cachedResponse.Data.AsStream(), true);
            }
            
            NSUrlResponse response;
            NSError error;
            NSData data = NSUrlConnection.SendSynchronousRequest(request, out response, out error);

            if (error != null || data == null)
            {
                return null;
            }

            cachedResponse = new NSCachedUrlResponse(response, data, null, NSUrlCacheStoragePolicy.Allowed);
            NSUrlCache.SharedCache.StoreCachedResponse(cachedResponse, request);

            return new Response(data.AsStream(), false);
        }
    }
}