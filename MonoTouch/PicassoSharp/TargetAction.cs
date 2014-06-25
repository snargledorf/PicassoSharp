using System;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public class TargetAction : Action
	{
		public TargetAction(Picasso picasso, 
		                    ITarget target, 
		                    Request data,
		                    bool skipCache,
		                    bool noFade,
		                    string key,
                            UIImage errorImage,
                            System.Action onSuccessListener,
                            System.Action onFailureListener,
                            System.Action onFinishListener)
            : base(picasso, target, data, skipCache, noFade, key, errorImage, onSuccessListener, onFailureListener, onFinishListener)
		{
		}

		#region implemented abstract members of Action

	    protected override void OnComplete(UIImage bitmap, LoadedFrom loadedFrom)
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
				target.OnImageFailed(ErrorImage);
		}
		#endregion
	}
}

