using System;
using System.Collections.Generic;

namespace PicassoSharp
{
    public interface IBitmapHunter<TBitmap, TError>
    {
        Request<TBitmap> Data { get; }
        TBitmap Result { get; }
        LoadedFrom LoadedFrom { get; }
        Exception Exception { get; }
        Action<TBitmap, TError> Action { get; }
        List<Action<TBitmap, TError>> Actions { get; }
    }
}