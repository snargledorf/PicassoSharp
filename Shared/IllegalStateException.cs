using System;

namespace PicassoSharp
{
    internal class IllegalStateException : Exception
    {
        public IllegalStateException(string message) : base(message)
        {
        }
    }
}