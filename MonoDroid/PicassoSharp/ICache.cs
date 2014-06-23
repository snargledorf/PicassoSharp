using System;
using Android.Graphics;

namespace PicassoSharp
{
	public interface ICache
	{
		void Set(string key, Bitmap image);

		Bitmap Get(String key);

		void Clear();
	}
}

