using System;

namespace PicassoSharp
{
	public interface ICache<T>
	{
		void Set(string key, T image);

		T Get(String key);

		void Clear();
	}
}

