using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PicassoSharp
{
    public interface IPicasso<TBitmap, TError>
    {
        IList<IRequestHandler<TBitmap>> RequestHandlers { get; }
        void CancelExistingRequest(object target);
        void Complete(IBitmapHunter<TBitmap, TError> hunter);
        void RunOnPicassoThread(Action action);
    }
}
