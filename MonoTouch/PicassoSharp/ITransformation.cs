using System;
using System.Collections.Generic;
using MonoTouch.UIKit;
using Exception = System.Exception;

namespace PicassoSharp
{
	public interface ITransformation
	{
		String Key
		{
			get;
		}

	    UIImage Transform(UIImage source);
	}

}

