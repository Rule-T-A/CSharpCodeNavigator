namespace Inheritance
{
	public abstract class AbstractBase
	{
		public abstract void Do();
	}

	public class Concrete : AbstractBase
	{
		public override void Do() {}
	}

	public class AbstractUse
	{
		public void Run()
		{
			AbstractBase obj = new Concrete();
			obj.Do();
		}
	}
}


