using System;
using Android.Views;
using Android.Widget;

namespace PicassoSharp
{
    public class DeferredRequestCreator : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly RequestCreator m_RequestCreator;
        private readonly WeakReference<ImageView> m_Target;

        public DeferredRequestCreator(RequestCreator requestCreator, ImageView target)
        {
            m_RequestCreator = requestCreator;
            m_Target = new WeakReference<ImageView>(target);
            target.ViewTreeObserver.AddOnPreDrawListener(this);
        }

        public bool OnPreDraw()
        {
            ImageView target;
            if (!m_Target.TryGetTarget(out target))
                return true;

            ViewTreeObserver vto = target.ViewTreeObserver;
            if (!vto.IsAlive)
                return true;

            int width = target.MeasuredWidth;
            int height = target.MeasuredHeight;

            if (width <= 0 || height <= 0)
                return true;

            vto.RemoveOnPreDrawListener(this);

            m_RequestCreator.Unfit().Resize(width, height).Into(target);

            return true;
        }

        public void Cancel()
        {
            ImageView target;
            if (!m_Target.TryGetTarget(out target))
                return;

            ViewTreeObserver vto = target.ViewTreeObserver;
            if (!vto.IsAlive)
                return;

            vto.RemoveOnPreDrawListener(this);
        }
    }
}