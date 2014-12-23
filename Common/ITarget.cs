
namespace PicassoSharp
{
    public interface ITarget<TBitmap, TPlaceholder, TError>
	{
        void OnPrepareLoad(TPlaceholder placeHolderDrawable);
        void OnImageLoaded(TBitmap bitmap, IPicasso<TBitmap, TError> picasso, LoadedFrom loadedFrom);
        void OnImageFailed(TError errorDrawable);
	}
}

