using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;

namespace PicassoSharp
{
    public class PicassoDrawable : BitmapDrawable
    {
        private const float FadeDuration = 200f; //ms

        internal static void SetPlaceholder(ImageView imageView, Drawable placeholderDrawable)
        {
            imageView.SetImageDrawable(placeholderDrawable);
        }

        internal static void SetBitmap(ImageView target, Context context, Bitmap bitmap, LoadedFrom loadedFrom, FadeMode fadeMode)
        {
            Drawable placeholder = target.Drawable;
            var drawable = new PicassoDrawable(context, bitmap, placeholder, loadedFrom, fadeMode);

            target.SetImageDrawable(drawable);
        }

        private Drawable m_Placeholder;
        private bool m_Animating;
        private long m_StartTimeMillis;
        private int m_Alpha = 0xFF;

        private PicassoDrawable(Context context, Bitmap bitmap, Drawable placeholder, LoadedFrom loadedFrom, FadeMode fadeMode)
            : base(context.Resources, bitmap)
        {
            bool fade = fadeMode == FadeMode.Always ||
                        (loadedFrom != LoadedFrom.Memory && fadeMode == FadeMode.NotFromMemory);
            if (fade)
            {
                m_Placeholder = placeholder;
                m_Animating = true;
                m_StartTimeMillis = SystemClock.UptimeMillis();
            }
        }

        public override void Draw(Canvas canvas)
        {
            if (!m_Animating)
            {
                base.Draw(canvas);
            }
            else
            {
                float normalized = (SystemClock.UptimeMillis() - m_StartTimeMillis) / FadeDuration;
                if (normalized >= 1f)
                {
                    m_Animating = false;
                    m_Placeholder = null;
                    base.Draw(canvas);
                }
                else
                {
                    if (m_Placeholder != null)
                    {
                        m_Placeholder.Draw(canvas);
                    }

                    int partialAlpha = (int)(m_Alpha * normalized);
                    SetAlpha(partialAlpha);
                    base.Draw(canvas);
                    SetAlpha(m_Alpha);
                }
            }
        }

        public override void SetAlpha(int alpha)
        {
            if (m_Placeholder != null)
            {
                m_Placeholder.SetAlpha(alpha);
            }
            base.SetAlpha(alpha);
        }

        public override void SetColorFilter(Color color, PorterDuff.Mode mode)
        {
            if (m_Placeholder != null)
            {
                m_Placeholder.SetColorFilter(color, mode);
            }
            base.SetColorFilter(color, mode);
        }

        protected override void OnBoundsChange(Rect bounds)
        {
            if (m_Placeholder != null)
            {
                m_Placeholder.SetBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            }
            base.OnBoundsChange(bounds);
        }
    }
}