using Android.Graphics.Drawables;
using Android.Graphics;

namespace PicassoSharp
{
    public abstract class Target : Java.Lang.Object
	{
		public abstract void OnPrepareLoad(Drawable placeHolderDrawable);
        public abstract void OnImageLoaded(Bitmap bitmap, Picasso picasso, LoadedFrom loadedFrom);
        public abstract void OnImageFailed(Drawable errorDrawable);
	}
}

