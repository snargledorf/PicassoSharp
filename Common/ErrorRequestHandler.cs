using System;

namespace PicassoSharp
{
    public class ErrorRequestHandler<TBitmap> : IRequestHandler<TBitmap>
    {

        public int RetryCount { get { return 0; } }

        public bool SupportsReplay { get { return false; } }

        public bool CanHandleRequest(Request<TBitmap> data)
        {
            return true;
        }

        public Result<TBitmap> Load(Request<TBitmap> data)
        {
            throw new InvalidOperationException("Unrecognized type of request: " + data);
        }
    }
}
