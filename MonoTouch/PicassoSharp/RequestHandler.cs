using System;
using MonoTouch.UIKit;

namespace PicassoSharp
{
    public abstract class RequestHandler : IRequestHandler<UIImage>
    {
        public virtual int RetryCount
        {
            get { return 0; }
        }

        public virtual bool SupportsReplay
        {
            get { return false; }
        }

        public abstract bool CanHandleRequest(Request<UIImage> data);

        public abstract Result<UIImage> Load(Request<UIImage> data);
    }
}