using System;
using MonoTouch.UIKit;

namespace PicassoSharp
{
	public class UIImageViewAction : Action
	{
        private readonly ICallback m_Callback;
		public UIImageViewAction(
            Picasso picasso, 
            UIImageView target, 
            Request data,
            bool skipCache,
            bool noFade,
            string key, 
            UIImage errorImage,
            ICallback callback)
			: base(picasso, target, data, skipCache, noFade, key, errorImage)
		{
            m_Callback = callback;
		}

		#region implemented abstract members of Action

		public override void Complete(UIImage bitmap, LoadedFrom loadedFrom)
		{
			if (bitmap == null) {
				throw new Exception(String.Format("Attempted to complete action with no result!\n{0}", this));
			}

			var target = this.Target as UIImageView;
			if (target == null)
				return;

            UIPicassoImage.SetImage(target, Picasso.Context, bitmap, loadedFrom, NoFade);

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

