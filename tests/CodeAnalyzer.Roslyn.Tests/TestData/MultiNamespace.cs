namespace First.Namespace
{
	public class Alpha
	{
		public void DoSomething() {}
		public static void DoStatic() {}
	}
}

namespace Second.Namespace.Inner
{
	public class Beta
	{
		public int Add(int x, int y) => x + y;
		public static string Name() => nameof(Beta);
	}
}
