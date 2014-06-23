using System.IO;
using System.Text;

namespace PicassoSharp
{
	internal sealed class Utils
	{
	    public const string ThreadPrefix = "Picasso-";
        public const string ThreadIdleName = ThreadPrefix + "Idle";

	    public static string CreateKey(Request request)
		{
			return string.Format("{0};{1};{2};", request.Uri, request.TargetWidth, request.TargetHeight);
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

