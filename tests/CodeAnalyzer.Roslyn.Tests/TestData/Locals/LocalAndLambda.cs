using System;

namespace Local.Lambda
{
	public class Uses
	{
		public void Run()
		{
			void Local()
			{
				Console.WriteLine("local");
			}

			Action lam = () => Console.WriteLine("lambda");
			Local();
			lam();
		}
	}
}


