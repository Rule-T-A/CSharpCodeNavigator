using System;
using System.Threading.Tasks;

namespace VectorStoreExplorer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Try to use the VectorStore package
                var assembly = System.Reflection.Assembly.LoadFrom(@"C:\Users\katie\.nuget\packages\vectorstore\1.0.0\lib\net8.0\VectorStore.dll");
                Console.WriteLine($"Assembly loaded: {assembly.FullName}");
                
                var types = assembly.GetTypes();
                Console.WriteLine("\nAvailable types:");
                foreach (var type in types)
                {
                    Console.WriteLine($"  {type.FullName}");
                    
                    // Show methods for interesting types
                    if (type.Name.Contains("VectorStore") || type.Name.Contains("File"))
                    {
                        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                        Console.WriteLine($"    Methods:");
                        foreach (var method in methods)
                        {
                            if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                            {
                                Console.WriteLine($"      {method.Name}({string.Join(", ", Array.ConvertAll(method.GetParameters(), p => $"{p.ParameterType.Name} {p.Name}"))})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
