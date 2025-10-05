namespace CodeAnalyzer.Roslyn.Models
{
	public class AnalyzerOptions
	{
		public bool IncludeWarningsInErrors { get; set; } = false;
		public bool RecordExternalCalls { get; set; } = true;
		public bool AttributeInitializerCalls { get; set; } = false;
	}
}


