using System;
using Android.Graphics;
using Android.Graphics.Drawables;

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
		                    Drawable errorDrawable)
			: base(picasso, target, data, skipCache, noFade, key, errorDrawable)
		{
		}

		#region implemented abstract members of Action

		public override void Complete(Bitmap bitmap, LoadedFrom loadedFrom)
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
				target.OnImageFailed(ErrorDrawable);
		}
		#endregion
	}
}

