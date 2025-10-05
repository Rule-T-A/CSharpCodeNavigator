namespace BaseQualifier
{
	public class Base
	{
		public virtual void Foo() {}
	}

	public class Derived : Base
	{
		public override void Foo() {}

		public void DoBoth()
		{
			base.Foo();
			this.Foo();
		}
	}
}


