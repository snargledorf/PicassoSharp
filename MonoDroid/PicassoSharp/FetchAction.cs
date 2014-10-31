using System;
using Android.Graphics;

namespace PicassoSharp
{
    public class FetchAction : Action
    {
        private readonly Object m_Target;

        public override object Target
        {
            get { return m_Target; }
        }

        public FetchAction(Picasso picasso, Request request, bool skipCache, string key) 
            : base(picasso, null, request, skipCache, FadeMode.Never, key, null, null, null, null)
        {
            m_Target = new Object();
        }

        protected override void OnComplete(Bitmap bitmap, LoadedFrom loadedFrom)
        {
        }

        protected override void OnError()
        {
        }
    }
}