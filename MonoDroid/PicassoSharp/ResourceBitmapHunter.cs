using Android.Content.Res;
using Android.Graphics;

namespace PicassoSharp
{
    internal class ResourceBitmapHunter : BitmapHunter
    {
        public ResourceBitmapHunter(Picasso picasso, Action action, Dispatcher dispatcher, ICache<Bitmap> cache)
            : base(picasso, action, dispatcher, cache)
        {
        }

        protected override Bitmap Decode(Request data)
        {
            return DecodeResource(Picasso.Context.Resources, data.ResourceId, data);
        }

        private static Bitmap DecodeResource(Resources resources, int id, Request data)
        {
            BitmapFactory.Options options = CreateBitmapOptions(data);
            if (RequiresInSampleSize(options))
            {
                BitmapFactory.DecodeResource(resources, id, options);
                CalculateInSampleSize(data.TargetWidth, data.TargetHeight, options, data);
            }
            return BitmapFactory.DecodeResource(resources, id, options);
        }
    }
}