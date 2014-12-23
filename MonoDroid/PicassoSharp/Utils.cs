using System.IO;
using System.Text;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PicassoSharp
{
	internal sealed class Utils
	{
	    public const string ThreadPrefix = "Picasso-";
        public const string ThreadIdleName = ThreadPrefix + "Idle";
        public const int DefaultConnectTimeout = 15 * 1000;
        public const int DefaultReadTimeout = 20 * 1000;

        private const int KeyPadding = 50; // Cloned from exact science

	    private static readonly StringBuilder s_MainThreadKeyBuilder = new StringBuilder();

	    public static string CreateKey(Request<Bitmap> data)
	    {
	        string key = CreateKey(data, s_MainThreadKeyBuilder);
	        s_MainThreadKeyBuilder.Length = 0;
            return key;
		}

	    public static string CreateKey(Request<Bitmap> data, StringBuilder builder)
	    {
	        if (!string.IsNullOrEmpty(data.StableKey))
	        {
	            builder.EnsureCapacity(data.StableKey.Length + KeyPadding);
	            builder.Append(data.StableKey);
	        } 
            else if (data.Uri != null)
	        {
	            string path = data.Uri.ToString();
                builder.EnsureCapacity(path.Length + KeyPadding);
	            builder.Append(path);
	        }
	        else
	        {
                builder.EnsureCapacity(KeyPadding);
                builder.Append(data.ResourceId);
	        }

	        builder.Append(';');

	        if (data.TargetWidth != 0)
	        {
	            builder.Append("resize:").Append(data.TargetWidth).Append('x').Append(data.TargetHeight);
	            builder.Append(';');
	        }

	        if (data.CenterCrop)
	        {
	            builder.Append("centercrop;");
	        }

	        if (data.CenterInside)
	        {
	            builder.Append("centerinside;");
	        }

            if (data.Transformations != null)
            {
                for (int i = 0; i < data.Transformations.Count; i++)
                {
                    builder.Append(data.Transformations[i].Key);
                    builder.Append(';');
                }
            }

            return builder.ToString();
	    }

	    public static void CloseQuietly(Stream stream)
	    {
	        if (stream == null)
	            return;
	        try
	        {
	            stream.Close();
	        }
	        catch (IOException)
	        {
	        }
	    }

	    public static byte[] ToByteArray(Stream stream)
	    {
	        using (var ms = new MemoryStream())
	        {
	            byte[] buffer = new byte[1024*4];
	            int bytesRead;
	            while (0 != (bytesRead = stream.Read(buffer, 0, buffer.Length)))
	            {
                    ms.Write(buffer, 0, bytesRead);
	            }
	            return ms.ToArray();
	        }
	    }

        public static IDownloader<Bitmap> CreateDefaultDownloader(Context context)
	    {
            // For now this just returns a UrlConnectionDownloader
            return new UrlConnectionDownloader(context);
	    }

        public static ObjectWrapper<T> Wrap<T>(T value)
        {
            return new ObjectWrapper<T>(value);
        }

        public static T Unwrap<T>(ObjectWrapper<T> value)
	    {
	        return value.Value;
	    }

	    public class ObjectWrapper<T> : Java.Lang.Object
	    {
            private readonly T m_Value;

            public ObjectWrapper(T value)
	        {
	            m_Value = value;
	        }

            public T Value
	        {
	            get { return m_Value; }
	        }
	    }
	}
}

