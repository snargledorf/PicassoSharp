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
		                    UIImage errorImage)
			: base(picasso, target, data, skipCache, noFade, key, errorImage)
		{
		}

		#region implemented abstract members of Action

		public override void Complete(UIImage bitmap, LoadedFrom loadedFrom)
		{
			if (bitmap == null) {
				throw new Exception(String.Format("Attempted to complete action with no result!\n{0}", this));
			}
			var target = this.Target as ITarget;
			if (target != null)
				target.OnImageLoaded(bitmap, Picasso, loadedFrom);
		}

		public override void Error()
		{
			var target = this.Target as ITarget;
			if (target != null)
				target.OnImageFailed(ErrorImage);
		}
		#endregion
	}
}

