namespace PicassoSharp
{
    public interface IRequestHandler<TBitmap>
    {
        int RetryCount { get; }

        bool SupportsReplay { get; }

        bool CanHandleRequest(Request<TBitmap> data);

        Result<TBitmap> Load(Request<TBitmap> data);
    }
}