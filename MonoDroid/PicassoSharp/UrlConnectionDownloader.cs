using System;
using Android.Content;
using Android.Net.Http;
using Android.OS;
using Java.IO;
using Java.Net;

namespace PicassoSharp
{
    class UrlConnectionDownloader : IDownloader
    {
        private const string ResponseSource = "X-Android-Response-Source";

        private static readonly object s_Lock = new object();

        private volatile object m_Cache;

        private readonly Context m_Context;

        public UrlConnectionDownloader(Context context)
        {
            m_Context = context.ApplicationContext;
        }

        private URLConnection OpenConnection(Uri uri)
        {
            var url = new URL(uri.AbsoluteUri);
            var connection = url.OpenConnection();
            connection.ConnectTimeout = Utils.DefaultConnectTimeout;
            connection.ReadTimeout = Utils.DefaultReadTimeout;
            return connection;
        }

        public Response Load(Uri uri, bool localCacheOnly)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                InstallCacheIfNeeded(m_Context);
            }

            URLConnection connection = OpenConnection(uri);
            connection.UseCaches = true;
            if (localCacheOnly)
            {
                connection.SetRequestProperty("Cache-Control", "only-if-cached,max-age=" + int.MaxValue);
            }

            var httpConnection = connection as HttpURLConnection;
            if (httpConnection != null)
            {
                int responseCode = (int) httpConnection.ResponseCode;
                if (responseCode >= 300)
                {
                    httpConnection.Disconnect();
                    throw new ResponseException(responseCode + " " + httpConnection.ResponseMessage);
                }
            }

            long contentLength = connection.GetHeaderFieldInt("Content-Length", -1);
            bool fromCache = ParseResponseSourceHeader(connection.GetHeaderField(ResponseSource));

            return new Response(connection.InputStream, fromCache, contentLength);
        }

        public void Shutdown()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich && m_Cache != null)
            {
                ResponseCacheIcs.Close(m_Cache);
            }
        }

        private bool ParseResponseSourceHeader(string header)
        {
            if (header == null)
            {
                return false;
            }
            String[] parts = header.Split(new char[]
            {
                ' '
            }
                , 2);
            if ("CACHE".Equals(parts[0]))
            {
                return true;
            }
            if (parts.Length == 1)
            {
                return false;
            }
            try
            {
                return "CONDITIONAL_CACHE".Equals(parts[0]) && int.Parse(parts[1]) == 304;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void InstallCacheIfNeeded(Context context)
        {
            if (m_Cache == null)
            {
                try
                {
                    lock (s_Lock)
                    {
                        if (m_Cache == null)
                        {
                            m_Cache = ResponseCacheIcs.Install(context);
                        }
                    }
                }
                catch (IOException)
                {
                    // Ignored
                }
            }
        }

        private static class ResponseCacheIcs
        {
            public static object Install(Context context)
            {
                File cacheDir = AndroidUtils.CreateDefaultCacheDir(context);
                HttpResponseCache cache = HttpResponseCache.Installed;
                if (cache == null)
                {
                    long maxSize = AndroidUtils.CalculateDiskCacheSize(cacheDir);
                    cache = HttpResponseCache.Install(cacheDir, maxSize);
                }
                return cache;
            }

            public static void Close(object cache)
            {
                try
                {
                    ((HttpResponseCache)cache).Close();
                }
                catch (IOException)
                {
                    // Ignored
                }
            }
        }
    }
}
