using System;

using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views.Animations;

namespace PicassoSharp
{
	public class ImageViewAction : Action
	{
        private readonly ICallback m_Callback;
		public ImageViewAction(
            Picasso picasso, 
            ImageView target, 
            Request data,
            bool skipCache,
            bool noFade,
            string key, 
            Drawable errorDrawable,
            ICallback callback)
			: base(picasso, target, data, skipCache, noFade, key, errorDrawable)
		{
            m_Callback = callback;
		}

		#region implemented abstract members of Action

		public override void Complete(Bitmap bitmap, LoadedFrom loadedFrom)
		{
			if (bitmap == null) {
				throw new Exception(String.Format("Attempted to complete action with no result!\n{0}", this));
			}

			var target = this.Target as ImageView;
			if (target == null)
				return;

            PicassoDrawable.SetBitmap(target, Picasso.Context, bitmap, loadedFrom, NoFade);

            if (m_Callback != null)
            {
                m_Callback.OnSuccess();
            }
		}

	    public override void Error()
	    {
            var target = this.Target as ImageView;
            if (target == null)
                return;

	        if (ErrorDrawable != null)
	        {
	            target.SetImageDrawable(ErrorDrawable);
	        }

	        if (m_Callback != null)
	        {
	            m_Callback.OnError();
	        }
	    }

	    #endregion
	}
}

