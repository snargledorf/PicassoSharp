using System;
using Android.Graphics;

namespace PicassoSharp
{
    public class GetAction : Action
    {
        public GetAction(Picasso picasso, Request request, bool skipCache, string key)
            : base(picasso, null, request, skipCache, FadeMode.Never, key, null, null, null, null)

        {
        }

        protected override void OnComplete(Bitmap bitmap, LoadedFrom loadedFrom)
        {
        }

        protected override void OnError()
        {
        }
    }
}