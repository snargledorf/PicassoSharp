namespace PicassoSharp
{
    public interface ITransformation<TBitmap>
    {
        TBitmap Transform(TBitmap source);
        string Key { get; }
    }
}