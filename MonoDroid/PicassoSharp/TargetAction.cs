using System;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PicassoSharp
{
	public class TargetAction : Action
	{
		public TargetAction(Picasso picasso, ITarget target, Request request, bool skipCache, FadeMode fadeMode, string key, Drawable errorDrawable, System.Action onSuccessListener, System.Action onFailureListener, System.Action onFinishListener)
			: base(picasso, target, request, skipCache, fadeMode, key, errorDrawable, onSuccessListener, onFailureListener, onFinishListener)
		{
		}

		#region implemented abstract members of Action

	    protected override void OnComplete(Bitmap bitmap, LoadedFrom loadedFrom)
		{
			if (bitmap == null) {
				throw new Exception(String.Format("Attempted to complete action with no result!\n{0}", this));
			}
			var target = this.Target as ITarget;
			if (target != null)
				target.OnImageLoaded(bitmap, Picasso, loadedFrom);
		}

	    protected override void OnError()
		{
			var target = this.Target as ITarget;
			if (target != null)
				target.OnImageFailed(ErrorDrawable);
		}
		#endregion
	}
}

