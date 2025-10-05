namespace CrossFile.Use
{
	using CrossFile.Svc;

	public class Consumer
	{
		private readonly IService _svc;

		public Consumer(IService svc)
		{
			_svc = svc;
		}

		public void Run()
		{
			_svc.Work();
			_svc.Extra();
		}
	}
}


