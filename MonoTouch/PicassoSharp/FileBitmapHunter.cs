using System.IO;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	class FileBitmapHunter : BitmapHunter
	{
		internal FileBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<UIImage> cache, IDownloader downloader) 
			: base(picasso, action, dispatcher, cache, downloader)
		{
		}

        protected override UIImage Decode(Request data)
		{
			LoadedFrom = LoadedFrom.Disk;

			return UIImage.FromFile(data.Uri.AbsolutePath);
		}
	}

}

