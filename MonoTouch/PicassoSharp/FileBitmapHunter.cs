using MonoTouch.UIKit;

namespace PicassoSharp
{
	class FileBitmapHunter : BitmapHunter
	{
		internal FileBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<UIImage> cache) 
			: base(picasso, action, dispatcher, cache)
		{
		}

        protected override UIImage Decode(Request data)
		{
			LoadedFrom = LoadedFrom.Disk;

			return UIImage.FromFile(data.Uri.AbsoluteUri);
		}
	}

}

