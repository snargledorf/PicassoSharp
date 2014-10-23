using System;

using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PicassoSharp
{
	public class ImageViewAction : Action
	{
		public ImageViewAction(Picasso picasso, ImageView target, Request data, bool skipCache, FadeMode fadeMode, string key, Drawable errorDrawable, System.Action onSuccessListener, System.Action onFailureListener, System.Action onFinishListener)
			: base(picasso, target, data, skipCache, fadeMode, key, errorDrawable, onSuccessListener, onFailureListener, onFinishListener)
		{
		}

		#region implemented abstract members of Action

		protected override void OnComplete(Bitmap bitmap, LoadedFrom loadedFrom)
		{
			if (bitmap == null) {
				throw new Exception(String.Format("Attempted to complete action with no result!\n{0}", this));
			}

			var target = this.Target as ImageView;
            if (target == null || target.Handle == IntPtr.Zero)
				return;

            PicassoDrawable.SetBitmap(target, Picasso.Context, bitmap, loadedFrom, FadeMode);
		}

	    protected override void OnError()
	    {
            var target = this.Target as ImageView;
            if (target == null || target.Handle == IntPtr.Zero)
                return;

	        if (ErrorDrawable != null)
	        {
	            target.SetImageDrawable(ErrorDrawable);
	        }
	    }

	    #endregion
	}
}

