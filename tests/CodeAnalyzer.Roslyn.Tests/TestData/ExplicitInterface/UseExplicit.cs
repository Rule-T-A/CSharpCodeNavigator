using Explicit.Contract;
using Explicit.Impl;

namespace Explicit
{
	public class Use
{
		public void Run()
		{
			IThing t = new Thing();
			t.Do();
		}
	}
}


