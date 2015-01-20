using MonoTouch.UIKit;

namespace PicassoSharp
{
	class FileRequestHandler : RequestHandler
	{
	    public override bool CanHandleRequest(Request<UIImage> data)
	    {
	        return data.Uri.IsFile;
	    }

	    public override Result<UIImage> Load(Request<UIImage> data)
        {
            return new Result<UIImage>(UIImage.FromFile(data.Uri.AbsolutePath), LoadedFrom.Disk);
	    }
	}

}

