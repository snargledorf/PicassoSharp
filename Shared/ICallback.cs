using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PicassoSharp
{
    public interface ICallback
    {
        void OnSuccess();
        void OnError();
    }
}
