using Android.Graphics;

namespace PicassoSharp
{
    public interface ITransformation
    {
        Bitmap Transform(Bitmap source);
        string Key { get; }
    }
}