using MonoTouch.UIKit;

namespace PicassoSharp
{
    public interface ITarget
	{
		void OnPrepareLoad(UIImage placeholderImage);
        void OnImageLoaded(UIImage image, Picasso picasso, LoadedFrom loadedFrom);
        void OnImageFailed(UIImage errorImage);
	}
}

