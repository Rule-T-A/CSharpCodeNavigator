using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeAnalyzer.Roslyn.Models
{
	public interface IVectorStoreWriter
	{
		Task<string> AddTextAsync(string content, Dictionary<string, object> metadata);
	}
}


