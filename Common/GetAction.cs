namespace PicassoSharp
{
    public class GetAction<TBitmap, TError> : Action<TBitmap, TError>
    {
        public GetAction(IPicasso<TBitmap, TError> picasso, Request<TBitmap> request, bool skipCache, string key)
            : base(picasso, null, request, skipCache, FadeMode.Never, key, default(TError), null, null, null)

        {
        }

        protected override void OnComplete(TBitmap bitmap, LoadedFrom loadedFrom)
        {
        }

        protected override void OnError()
        {
        }
    }
}