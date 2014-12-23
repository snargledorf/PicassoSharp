using System;

namespace PicassoSharp
{
    public class TargetAction<TBitmap, TPlaceholder, TError> : Action<TBitmap, TError>
	{
        public TargetAction(IPicasso<TBitmap, TError> picasso, ITarget<TBitmap, TPlaceholder, TError> target, Request<TBitmap> request, bool skipCache, FadeMode fadeMode, string key, TError errorDrawable, System.Action onSuccessListener, System.Action onFailureListener, System.Action onFinishListener)
			: base(picasso, target, request, skipCache, fadeMode, key, errorDrawable, onSuccessListener, onFailureListener, onFinishListener)
		{
		}

		#region implemented abstract members of Action

        protected override void OnComplete(TBitmap bitmap, LoadedFrom loadedFrom)
		{
			if (bitmap == null) {
				throw new Exception(String.Format("Attempted to complete action with no result!\n{0}", this));
			}
			var target = this.Target as ITarget<TBitmap, TPlaceholder, TError>;
			if (target != null)
				target.OnImageLoaded(bitmap, Picasso, loadedFrom);
		}

	    protected override void OnError()
		{
            var target = this.Target as ITarget<TBitmap, TPlaceholder, TError>;
			if (target != null)
				target.OnImageFailed(ErrorDrawable);
		}
		#endregion
	}
}

