using MonoTouch.UIKit;

namespace PicassoSharp
{
    public class UIImageViewSpinnerTarget : ITarget<UIImage, UIImage, UIImage>
    {
        private readonly UIImageView m_ImageView;
        private readonly UIColor m_OriginalBackgroundColor;
        private UIActivityIndicatorView m_Spinner;

        public UIImageViewSpinnerTarget(UIImageView imageView)
        {
            m_ImageView = imageView;
            m_OriginalBackgroundColor = m_ImageView.BackgroundColor;
        }

        public void OnPrepareLoad(UIImage placeholderImage)
        {
            SetBackgroundColor(UIColor.LightGray);
            AddSpinner();
        }

        public void OnImageLoaded(UIImage image, IPicasso<UIImage, UIImage> picasso, LoadedFrom loadedFrom)
        {
            ResetBackgroundColor();
            RemoveSpinner();
			m_ImageView.Image = image;
        }

        public void OnImageFailed(UIImage errorImage)
        {
            ResetBackgroundColor();
            RemoveSpinner();
        }

        private void ResetBackgroundColor()
        {
            SetBackgroundColor(m_OriginalBackgroundColor);
        }

        private void SetBackgroundColor(UIColor uiColor)
        {
            m_ImageView.BackgroundColor = uiColor;
        }

        private void AddSpinner()
        {
            if (m_Spinner != null) return;

            m_Spinner = new UIActivityIndicatorView();
            m_Spinner.Frame = new System.Drawing.RectangleF(new System.Drawing.PointF(0, 0), m_ImageView.Frame.Size);
            m_Spinner.StartAnimating();
            m_ImageView.AddSubview(m_Spinner);
        }

        private void RemoveSpinner()
        {
            if (m_Spinner == null)
                return;

            m_Spinner.StopAnimating();
            m_Spinner.RemoveFromSuperview();
            m_Spinner = null;
        }
    }
}