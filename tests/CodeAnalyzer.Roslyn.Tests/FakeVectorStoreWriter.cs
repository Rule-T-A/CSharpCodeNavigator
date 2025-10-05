using System.Collections.Generic;
using System.Threading.Tasks;
using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests
{
	public class FakeVectorStoreWriter : IVectorStoreWriter
	{
		public List<(string content, Dictionary<string, object> metadata)> Writes { get; } = new();

		public Task<string> AddTextAsync(string content, Dictionary<string, object> metadata)
		{
			Writes.Add((content, metadata));
			return Task.FromResult(System.Guid.NewGuid().ToString("N"));
		}
	}
}


