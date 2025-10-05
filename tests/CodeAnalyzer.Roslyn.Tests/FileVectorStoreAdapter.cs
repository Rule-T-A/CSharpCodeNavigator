using System.Collections.Generic;
using System.Threading.Tasks;
using CodeAnalyzer.Roslyn.Models;
using System.Reflection;

namespace CodeAnalyzer.Roslyn.Tests
{
	public class FileVectorStoreAdapter : IVectorStoreWriter
	{
		private readonly dynamic _store;

		public FileVectorStoreAdapter(object store)
		{
			_store = store;
		}

		public Task<string> AddTextAsync(string content, Dictionary<string, object> metadata)
		{
			return _store.AddTextAsync(content, metadata);
		}
	}
}
