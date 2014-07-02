using Android.Graphics.Drawables;
using Android.Graphics;

namespace PicassoSharp
{
    public interface ITarget
	{
		void OnPrepareLoad(Drawable placeHolderDrawable);
        void OnImageLoaded(Bitmap bitmap, Picasso picasso, LoadedFrom loadedFrom);
        void OnImageFailed(Drawable errorDrawable);
	}
}

