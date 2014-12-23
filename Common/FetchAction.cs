using System;

namespace PicassoSharp
{
    public class FetchAction<TBitmap, TError> : Action<TBitmap, TError>
    {
        private readonly Object m_Target;

        public override object Target
        {
            get { return m_Target; }
        }

        public FetchAction(IPicasso<TBitmap, TError> picasso, Request<TBitmap> request, bool skipCache, string key)
            : base(picasso, null, request, skipCache, FadeMode.Never, key, default(TError), null, null, null)
        {
            m_Target = new Object();
        }

        protected override void OnComplete(TBitmap bitmap, LoadedFrom loadedFrom)
        {
        }

        protected override void OnError()
        {
        }
    }
}