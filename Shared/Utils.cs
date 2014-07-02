using System.IO;
using System.Text;

namespace PicassoSharp
{
	internal sealed class Utils
	{
	    public const string ThreadPrefix = "Picasso-";
        public const string ThreadIdleName = ThreadPrefix + "Idle";

	    private static readonly StringBuilder MainThreadKeyBuilder = new StringBuilder();

	    public static string CreateKey(Request request)
	    {
	        string key = CreateKey(request, MainThreadKeyBuilder);
	        MainThreadKeyBuilder.Length = 0;
            return key;
		}

	    public static string CreateKey(Request request, StringBuilder builder)
	    {
	        string path = request.Uri.ToString();
            builder.EnsureCapacity(path.Length + 50);
	        builder.Append(path);

	        builder.Append(';');

	        if (request.TargetWidth != 0)
	        {
	            builder.Append("resize:").Append(request.TargetWidth).Append('x').Append(request.TargetHeight);
	            builder.Append(';');
	        }

	        if (request.CenterCrop)
	        {
	            builder.Append("centercrop;");
	        }

	        if (request.CenterInside)
	        {
	            builder.Append("centerinside;");
	        }

            if (request.Transformations != null)
            {
                for (int i = 0; i < request.Transformations.Count; i++)
                {
                    builder.Append(request.Transformations[i].Key);
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
	        using (MemoryStream ms = new MemoryStream())
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
	}
}

