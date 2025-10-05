namespace Virtual.Dispatch
{
	public class Base
	{
		public virtual void Do() {}
	}

	public class Derived : Base
	{
		public override void Do() {}
	}

	public class Uses
	{
		public void Run()
		{
			Base b = new Derived();
			b.Do();
		}
	}
}


