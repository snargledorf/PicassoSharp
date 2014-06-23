using System;
using System.IO;
using System.Net;

namespace PicassoSharp
{
    class WebClientDownloader : IDownloader
    {
        public Response Load(Uri uri)
        {
            var webClient = new WebClient();
            Stream bitmapStream = webClient.OpenRead(uri);
            return new Response(bitmapStream);
        }
    }
}