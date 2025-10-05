using System;

namespace Sample.Calls
{
	public class Caller
	{
		public void A()
		{
			B();
			Console.WriteLine("hi");
			Helper.Util();
		}

		public void B()
		{
			C();
		}

		private void C() {}
	}

	public static class Helper
	{
		public static void Util() {}
	}
}


