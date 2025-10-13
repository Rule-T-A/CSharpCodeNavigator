using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;
using VectorStore.Core;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CodeAnalyzer.Console;

/// <summary>
/// REPL Console Application for C# Code Navigator
/// Provides interactive commands for vector store management and code analysis
/// </summary>
class Program
{
    private static FileVectorStoreAdapter? _vectorStore;
    private static RoslynAnalyzer? _analyzer;
    private static string _storePath = "./vector-store";
    private static bool _running = true;
    private static VerbosityLevel _verbosity = VerbosityLevel.Terse;

    static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== C# Code Navigator REPL ===");
        System.Console.WriteLine("Type 'help' for available commands or 'exit' to quit.");
        System.Console.WriteLine();

        // Initialize vector store
        await InitializeVectorStoreAsync();

        // Main REPL loop
        while (_running)
        {
            System.Console.Write("> ");
            var input = System.Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            try
            {
                await ProcessCommandAsync(input.Trim());
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine();
            }
        }
    }

    private static async Task InitializeVectorStoreAsync()
    {
        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Initializing vector store at: {_storePath}");
            }
            
            _vectorStore = await FileVectorStoreAdapter.CreateAsync(_storePath, _verbosity);
            _analyzer = new RoslynAnalyzer(_vectorStore);
            
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine("Vector store initialized successfully.");
                System.Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to initialize vector store: {ex.Message}");
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine("Some commands may not work properly.");
                System.Console.WriteLine();
            }
        }
    }

    private static string[] ParseCommandLine(string command)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        for (int i = 0; i < command.Length; i++)
        {
            var c = command[i];

            if (c == '"' || c == '\'')
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result.ToArray();
    }

    private static async Task ProcessCommandAsync(string command)
    {
        var parts = ParseCommandLine(command);
        if (parts.Length == 0)
            return;
            
        var cmd = parts[0].ToLowerInvariant();

        switch (cmd)
        {
            case "help":
                ShowHelp();
                break;

            case "exit":
            case "quit":
                _running = false;
                System.Console.WriteLine("Goodbye!");
                break;

            case "clear":
                await ClearVectorStoreAsync();
                break;

            case "analyze":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: analyze <project-path>");
                    System.Console.WriteLine("Example: analyze ./MyProject/MyProject.csproj");
                    break;
                }
                await AnalyzeProjectAsync(parts[1]);
                break;

            case "search":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: search <query>");
                    System.Console.WriteLine("Example: search authentication methods");
                    break;
                }
                var query = string.Join(" ", parts.Skip(1));
                await SearchAsync(query);
                break;

            case "status":
                await ShowStatusAsync();
                break;

            case "store":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine($"Current store path: {_storePath}");
                    System.Console.WriteLine("Usage: store <new-path>");
                    break;
                }
                await ChangeStorePathAsync(parts[1]);
                break;

            case "verbosity":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine($"Current verbosity: {_verbosity}");
                    System.Console.WriteLine("Usage: verbosity <terse|normal|verbose>");
                    break;
                }
                SetVerbosity(parts[1]);
                break;

            default:
                System.Console.WriteLine($"Unknown command: {cmd}");
                System.Console.WriteLine("Type 'help' for available commands.");
                break;
        }
    }

    private static void ShowHelp()
    {
        System.Console.WriteLine("Available commands:");
        System.Console.WriteLine("  help                    - Show this help message");
        System.Console.WriteLine("  exit/quit              - Exit the application");
        System.Console.WriteLine("  clear                  - Clear all data from the vector store");
        System.Console.WriteLine("  analyze <project-path> - Analyze a C# project and store method calls");
        System.Console.WriteLine("  search <query>         - Search for method calls using semantic search");
        System.Console.WriteLine("  status                 - Show current status and statistics");
        System.Console.WriteLine("  store <path>           - Change vector store path and reinitialize");
        System.Console.WriteLine("  verbosity <level>      - Set output verbosity (terse|normal|verbose)");
        System.Console.WriteLine();
        System.Console.WriteLine("Examples:");
        System.Console.WriteLine("  analyze ./MyProject/MyProject.csproj");
        System.Console.WriteLine("  analyze C:\\Projects\\MyApp");
        System.Console.WriteLine("  search login authentication");
        System.Console.WriteLine("  search database connection");
        System.Console.WriteLine("  store ./my-custom-store");
        System.Console.WriteLine("  verbosity terse");
        System.Console.WriteLine();
    }

    private static void SetVerbosity(string level)
    {
        switch (level.ToLowerInvariant())
        {
            case "terse":
                _verbosity = VerbosityLevel.Terse;
                System.Console.WriteLine("Verbosity set to terse (minimal output)");
                break;
            case "normal":
                _verbosity = VerbosityLevel.Normal;
                System.Console.WriteLine("Verbosity set to normal (standard output)");
                break;
            case "verbose":
                _verbosity = VerbosityLevel.Verbose;
                System.Console.WriteLine("Verbosity set to verbose (detailed output)");
                break;
            default:
                System.Console.WriteLine($"Invalid verbosity level: {level}");
                System.Console.WriteLine("Valid levels: terse, normal, verbose");
                break;
        }
        System.Console.WriteLine();
    }

    private static async Task ClearVectorStoreAsync()
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine("Clearing vector store...");
            }
            
            // Get all document IDs and delete them
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var deletedCount = 0;
            
            foreach (var id in allIds)
            {
                var deleted = await _vectorStore.Store.DeleteAsync(id);
                if (deleted) deletedCount++;
            }

            if (_verbosity >= VerbosityLevel.Terse)
            {
                System.Console.WriteLine($"Cleared {deletedCount} documents from vector store.");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error clearing vector store: {ex.Message}");
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }

    private static async Task AnalyzeProjectAsync(string projectPath)
    {
        if (_analyzer == null)
        {
            System.Console.WriteLine("Analyzer not initialized.");
            return;
        }

        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Analyzing project: {projectPath}");
                System.Console.WriteLine("This may take a moment...");
            }
            
            var result = await _analyzer.AnalyzeProjectAsync(projectPath);
            
            if (_verbosity >= VerbosityLevel.Terse)
            {
                System.Console.WriteLine($"Analysis complete! Files: {result.FilesProcessed}, Methods: {result.MethodsAnalyzed}, Calls: {result.MethodCalls.Count}");
            }
            
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Analysis complete!");
                System.Console.WriteLine($"  Files processed: {result.FilesProcessed}");
                System.Console.WriteLine($"  Methods analyzed: {result.MethodsAnalyzed}");
                System.Console.WriteLine($"  Method calls found: {result.MethodCalls.Count}");
                
                if (result.Errors.Count > 0)
                {
                    System.Console.WriteLine($"  Errors encountered: {result.Errors.Count}");
                    foreach (var error in result.Errors.Take(5)) // Show first 5 errors
                    {
                        System.Console.WriteLine($"    - {error}");
                    }
                    if (result.Errors.Count > 5)
                    {
                        System.Console.WriteLine($"    ... and {result.Errors.Count - 5} more errors");
                    }
                }

                // Show sample method calls
                if (result.MethodCalls.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Sample method calls:");
                    foreach (var call in result.MethodCalls.Take(3))
                    {
                        System.Console.WriteLine($"  {call.Caller} -> {call.Callee}");
                        System.Console.WriteLine($"    File: {call.FilePath}:{call.LineNumber}");
                    }
                    if (result.MethodCalls.Count > 3)
                    {
                        System.Console.WriteLine($"  ... and {result.MethodCalls.Count - 3} more calls");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error analyzing project: {ex.Message}");
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }

    private static async Task SearchAsync(string query)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Searching for: {query}");
            }
            
            var results = await _vectorStore.Store.SearchTextAsync(query, limit: 3);
            
            if (results.Length == 0)
            {
                System.Console.WriteLine("No results found.");
            }
            else
            {
                if (_verbosity >= VerbosityLevel.Terse)
                {
                    System.Console.WriteLine($"Found {results.Length} results:");
                }
                
                if (_verbosity >= VerbosityLevel.Normal)
                {
                    System.Console.WriteLine($"Found {results.Length} results:");
                    System.Console.WriteLine();
                }
                
                for (int i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    var metadata = result.Document.Metadata;
                    
                    if (_verbosity >= VerbosityLevel.Terse)
                    {
                        System.Console.WriteLine($"{i + 1}. Similarity: {result.Similarity:F3}");
                        System.Console.WriteLine($"   Content: {result.Document.Content}");
                        
                        if (metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                        {
                            System.Console.WriteLine($"   Call: {metadata["caller"]} -> {metadata["callee"]}");
                        }
                        
                        if (metadata.ContainsKey("file_path") && metadata.ContainsKey("line_number"))
                        {
                            System.Console.WriteLine($"   Location: {metadata["file_path"]}:{metadata["line_number"]}");
                        }
                        
                        System.Console.WriteLine();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error searching: {ex.Message}");
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }

    private static async Task ShowStatusAsync()
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            
            if (_verbosity >= VerbosityLevel.Terse)
            {
                System.Console.WriteLine($"Status: {allIds.Length} documents, Path: {_storePath}");
            }
            
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Vector store status:");
                System.Console.WriteLine($"  Path: {_storePath}");
                System.Console.WriteLine($"  Documents: {allIds.Length}");
                
                if (allIds.Length > 0)
                {
                    // Count method call documents
                    var methodCallCount = 0;
                    foreach (var id in allIds)
                    {
                        var doc = await _vectorStore.Store.GetAsync(id);
                        if (doc?.Metadata.ContainsKey("type") == true && 
                            doc.Metadata["type"].ToString() == "method_call")
                        {
                            methodCallCount++;
                        }
                    }
                    
                    System.Console.WriteLine($"  Method calls: {methodCallCount}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting status: {ex.Message}");
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }

    private static async Task ChangeStorePathAsync(string newPath)
    {
        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Changing store path to: {newPath}");
            }
            
            // Dispose current store
            _vectorStore?.Dispose();
            
            // Update path and reinitialize
            _storePath = newPath;
            await InitializeVectorStoreAsync();
            
            if (_verbosity >= VerbosityLevel.Terse)
            {
                System.Console.WriteLine("Store path changed successfully.");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error changing store path: {ex.Message}");
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine("Reverting to previous path...");
            }
            
            // Try to revert
            try
            {
                await InitializeVectorStoreAsync();
            }
            catch
            {
                System.Console.WriteLine("Failed to revert. Please restart the application.");
            }
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }
}
