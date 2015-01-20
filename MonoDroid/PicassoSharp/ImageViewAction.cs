using System;

using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PicassoSharp
{
	public class ImageViewAction : Action<Bitmap, Drawable>
	{
	    private readonly Picasso m_Picasso;

		public ImageViewAction(Picasso picasso, ImageView target, Request<Bitmap> request, bool skipCache, FadeMode fadeMode, string key, Drawable errorImage, System.Action onSuccessListener, System.Action onFailureListener, System.Action onFinishListener)
			: base(picasso, target, request, skipCache, fadeMode, key, errorImage, onSuccessListener, onFailureListener, onFinishListener)
		{
		    m_Picasso = picasso;
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

            PicassoDrawable.SetBitmap(target, m_Picasso.Context, bitmap, loadedFrom, FadeMode);
		}

	    protected override void OnError()
	    {
            var target = this.Target as ImageView;
            if (target == null || target.Handle == IntPtr.Zero)
                return;

	        if (ErrorImage != null)
	        {
	            target.SetImageDrawable(ErrorImage);
	        }
	    }

	    #endregion
	}
}

