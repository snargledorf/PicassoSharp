using Android.Content;
using Android.Content.Res;
using Android.Graphics;

namespace PicassoSharp
{
    internal class ResourceRequestHandler : RequestHandler
    {
        private readonly Context m_Context;

        public ResourceRequestHandler(Context context)
        {
            m_Context = context;
        }

        public override bool CanHandleRequest(Request<Bitmap> data)
        {
            return data.ResourceId != 0;
        }

        public override Result<Bitmap> Load(Request<Bitmap> data)
        {
            return new Result<Bitmap>(DecodeResource(m_Context.Resources, data.ResourceId, data), LoadedFrom.Disk);
        }

        private static Bitmap DecodeResource(Resources resources, int id, Request<Bitmap> data)
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