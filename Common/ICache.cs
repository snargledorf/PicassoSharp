using System;

namespace PicassoSharp
{
	public interface ICache<T>
	{
		void Set(string key, T value);

		T Get(String key);

		void Clear();
	}
}

