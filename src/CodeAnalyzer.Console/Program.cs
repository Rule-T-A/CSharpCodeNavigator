using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;
using VectorStore.Core;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Linq;

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
        // Initialize vector store
        await InitializeVectorStoreAsync();

        // If command-line arguments provided, execute command and exit
        if (args.Length > 0)
        {
            var command = string.Join(" ", args);
            try
            {
                await ProcessCommandAsync(command);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                if (_verbosity >= VerbosityLevel.Verbose)
                {
                    System.Console.WriteLine(ex.StackTrace);
                }
                Environment.Exit(1);
            }
            return;
        }

        // Otherwise, run in REPL mode
        System.Console.WriteLine("=== C# Code Navigator REPL ===");
        System.Console.WriteLine("Type 'help' for available commands or 'exit' to quit.");
        System.Console.WriteLine();

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
            case "search-code":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: search <query> [--limit <n>] [--min-similarity <0.0-1.0>]");
                    System.Console.WriteLine("Example: search authentication methods --limit 10 --min-similarity 0.7");
                    break;
                }
                var queryParts = parts.Skip(1).ToList();
                var searchQuery = "";
                int searchLimit = 10;
                float minSimilarity = 0.0f;
                
                // Parse optional arguments
                for (int i = 0; i < queryParts.Count; i++)
                {
                    if (queryParts[i] == "--limit" && i + 1 < queryParts.Count && int.TryParse(queryParts[i + 1], out var l))
                    {
                        searchLimit = l;
                        queryParts.RemoveAt(i);
                        queryParts.RemoveAt(i);
                        i--;
                    }
                    else if (queryParts[i] == "--min-similarity" && i + 1 < queryParts.Count && float.TryParse(queryParts[i + 1], out var s))
                    {
                        minSimilarity = s;
                        queryParts.RemoveAt(i);
                        queryParts.RemoveAt(i);
                        i--;
                    }
                }
                searchQuery = string.Join(" ", queryParts);
                await SearchAsync(searchQuery, searchLimit, minSimilarity);
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

            case "validate":
            case "accuracy":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: validate <project-path>");
                    System.Console.WriteLine("Example: validate ./MyProject/MyProject.csproj");
                    System.Console.WriteLine("Measures the accuracy of stored data by comparing with source code.");
                    break;
                }
                await ValidateAccuracyAsync(parts[1]);
                break;

            case "validate-api":
            case "test-api":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: validate-api <project-path>");
                    System.Console.WriteLine("Example: validate-api ./MyProject/MyProject.csproj");
                    System.Console.WriteLine("Measures the accuracy of each API command by testing against ground truth.");
                    break;
                }
                await ValidateApiCommandsAsync(parts[1]);
                break;

            case "cleanup":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: cleanup <project-path>");
                    System.Console.WriteLine("Example: cleanup ./MyProject/MyProject.csproj");
                    System.Console.WriteLine("Removes stale data from vector store that doesn't exist in current code.");
                    break;
                }
                await CleanupStaleDataAsync(parts[1]);
                break;

            // API-equivalent commands
            case "list-classes":
                await ListClassesAsync(parts.Skip(1).ToArray());
                break;

            case "list-methods":
                await ListMethodsAsync(parts.Skip(1).ToArray());
                break;

            case "list-entry-points":
                await ListEntryPointsAsync(parts.Skip(1).ToArray());
                break;

            case "diff-project":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: diff-project <project-path> [--types <method_calls|method_definitions|class_definitions>] [--include-details]");
                    System.Console.WriteLine("Example: diff-project ./MyProject/MyProject.csproj --types method_calls method_definitions");
                    System.Console.WriteLine("Compares current codebase with stored database to find differences.");
                    break;
                }
                await DiffProjectAsync(parts.Skip(1).ToArray());
                break;

            case "list-projects":
                await ListProjectsAsync();
                break;

            case "get-method":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: get-method <fully-qualified-method-name>");
                    System.Console.WriteLine("Example: get-method MyApp.Services.UserService.ValidateUser");
                    break;
                }
                await GetMethodAsync(string.Join(" ", parts.Skip(1)));
                break;

            case "get-class":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: get-class <fully-qualified-class-name>");
                    System.Console.WriteLine("Example: get-class MyApp.Services.UserService");
                    break;
                }
                await GetClassAsync(string.Join(" ", parts.Skip(1)));
                break;

            case "get-callers":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: get-callers <fully-qualified-method-name> [depth]");
                    System.Console.WriteLine("Example: get-callers MyApp.Services.UserService.ValidateUser 1");
                    break;
                }
                var depth = parts.Length > 2 && int.TryParse(parts[2], out var d) ? d : 1;
                await GetCallersAsync(string.Join(" ", parts.Skip(1).Take(1)), depth);
                break;

            case "get-callees":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: get-callees <fully-qualified-method-name> [depth]");
                    System.Console.WriteLine("Example: get-callees MyApp.Services.UserService.ValidateUser 1");
                    break;
                }
                var calleeDepth = parts.Length > 2 && int.TryParse(parts[2], out var cd) ? cd : 1;
                await GetCalleesAsync(string.Join(" ", parts.Skip(1).Take(1)), calleeDepth);
                break;

            case "get-class-methods":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: get-class-methods <fully-qualified-class-name>");
                    System.Console.WriteLine("Example: get-class-methods MyApp.Services.UserService");
                    break;
                }
                await GetClassMethodsAsync(string.Join(" ", parts.Skip(1)));
                break;

            case "get-class-references":
                if (parts.Length < 2)
                {
                    System.Console.WriteLine("Usage: get-class-references <fully-qualified-class-name>");
                    System.Console.WriteLine("Example: get-class-references MyApp.Services.UserService");
                    break;
                }
                await GetClassReferencesAsync(string.Join(" ", parts.Skip(1)));
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
        System.Console.WriteLine("  validate <project>     - Measure accuracy of stored data vs source code");
        System.Console.WriteLine("  validate-api <project> - Measure accuracy of each API command vs ground truth");
        System.Console.WriteLine("  cleanup <project>      - Remove stale data not in current codebase");
        System.Console.WriteLine();
        System.Console.WriteLine("API-equivalent commands (for testing):");
        System.Console.WriteLine("  list-classes [--namespace <ns>] [--limit <n>] [--offset <n>]");
        System.Console.WriteLine("  list-methods [--class <name>] [--namespace <ns>] [--limit <n>] [--offset <n>]");
        System.Console.WriteLine("  list-entry-points");
        System.Console.WriteLine("  get-method <fully-qualified-method-name>");
        System.Console.WriteLine("  get-class <fully-qualified-class-name>");
        System.Console.WriteLine("  get-callers <method-fqn> [depth]");
        System.Console.WriteLine("  get-callees <method-fqn> [depth]");
        System.Console.WriteLine("  get-class-methods <class-fqn>");
        System.Console.WriteLine("  get-class-references <class-fqn>");
        System.Console.WriteLine("  diff-project <project-path> [--types <types>] [--include-details]");
        System.Console.WriteLine("  list-projects");
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
                System.Console.WriteLine($"Analysis complete! Files: {result.FilesProcessed}, Methods: {result.MethodsAnalyzed}, Calls: {result.MethodCalls.Count}, Definitions: {result.MethodDefinitions.Count}, Classes: {result.ClassDefinitions.Count}, Properties: {result.PropertyDefinitionCount}, Fields: {result.FieldDefinitionCount}, Enums: {result.EnumDefinitionCount}, Interfaces: {result.InterfaceDefinitionCount}, Structs: {result.StructDefinitionCount}");
            }
            
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Analysis complete!");
                System.Console.WriteLine($"  Files processed: {result.FilesProcessed}");
                System.Console.WriteLine($"  Methods analyzed: {result.MethodsAnalyzed}");
                System.Console.WriteLine($"  Method calls found: {result.MethodCalls.Count}");
                System.Console.WriteLine($"  Method definitions found: {result.MethodDefinitions.Count}");
                System.Console.WriteLine($"  Class definitions found: {result.ClassDefinitions.Count}");
                System.Console.WriteLine($"  Property definitions found: {result.PropertyDefinitionCount}");
                System.Console.WriteLine($"  Field definitions found: {result.FieldDefinitionCount}");
                System.Console.WriteLine($"  Enum definitions found: {result.EnumDefinitionCount}");
                System.Console.WriteLine($"  Interface definitions found: {result.InterfaceDefinitionCount}");
                System.Console.WriteLine($"  Struct definitions found: {result.StructDefinitionCount}");
                
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

                // Show sample method definitions
                if (result.MethodDefinitions.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Sample method definitions:");
                    foreach (var methodDef in result.MethodDefinitions.Take(3))
                    {
                        var parameters = string.Join(", ", methodDef.Parameters);
                        var modifiers = "";
                        if (methodDef.IsStatic) modifiers += "static ";
                        if (methodDef.IsVirtual) modifiers += "virtual ";
                        if (methodDef.IsAbstract) modifiers += "abstract ";
                        if (methodDef.IsOverride) modifiers += "override ";
                        
                        System.Console.WriteLine($"  {methodDef.AccessModifier} {modifiers.Trim()}{methodDef.ReturnType} {methodDef.MethodName}({parameters})");
                        System.Console.WriteLine($"    Class: {methodDef.ClassName}, Namespace: {methodDef.Namespace}");
                        System.Console.WriteLine($"    File: {methodDef.FilePath}:{methodDef.LineNumber}");
                    }
                    if (result.MethodDefinitions.Count > 3)
                    {
                        System.Console.WriteLine($"  ... and {result.MethodDefinitions.Count - 3} more definitions");
                    }
                }

                // Show sample class definitions
                if (result.ClassDefinitions.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Sample class definitions:");
                    foreach (var classDef in result.ClassDefinitions.Take(3))
                    {
                        var modifiers = "";
                        if (classDef.IsStatic) modifiers += "static ";
                        if (classDef.IsAbstract) modifiers += "abstract ";
                        if (classDef.IsSealed) modifiers += "sealed ";
                        
                        var inheritance = "";
                        if (!string.IsNullOrEmpty(classDef.BaseClass))
                        {
                            inheritance += $" : {classDef.BaseClass}";
                        }
                        if (classDef.Interfaces.Count > 0)
                        {
                            inheritance += (string.IsNullOrEmpty(classDef.BaseClass) ? " : " : ", ") + string.Join(", ", classDef.Interfaces);
                        }
                        
                        System.Console.WriteLine($"  {classDef.AccessModifier} {modifiers.Trim()}{classDef.ClassName}{inheritance}");
                        System.Console.WriteLine($"    Namespace: {classDef.Namespace}");
                        System.Console.WriteLine($"    Members: {classDef.MethodCount} methods, {classDef.PropertyCount} properties, {classDef.FieldCount} fields");
                        System.Console.WriteLine($"    File: {classDef.FilePath}:{classDef.LineNumber}");
                    }
                    if (result.ClassDefinitions.Count > 3)
                    {
                        System.Console.WriteLine($"  ... and {result.ClassDefinitions.Count - 3} more classes");
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

    private static async Task SearchAsync(string query, int limit = 10, float minSimilarity = 0.0f)
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
                System.Console.WriteLine($"Searching for: {query} (limit: {limit}, min similarity: {minSimilarity:F2})");
            }
            
            var results = await _vectorStore.Store.SearchTextAsync(query, limit: limit);
            
            // Filter by minimum similarity
            var filteredResults = results.Where(r => r.Similarity >= minSimilarity).ToArray();
            
            if (filteredResults.Length == 0)
            {
                System.Console.WriteLine($"No results found (searched {results.Length} results, filtered to {filteredResults.Length} above threshold {minSimilarity:F2}).");
            }
            else
            {
                if (_verbosity >= VerbosityLevel.Terse)
                {
                    System.Console.WriteLine($"Found {filteredResults.Length} results:");
                }
                
                if (_verbosity >= VerbosityLevel.Normal)
                {
                    System.Console.WriteLine($"Found {filteredResults.Length} results (from {results.Length} total):");
                    System.Console.WriteLine();
                }
                
                for (int i = 0; i < filteredResults.Length; i++)
                {
                    var result = filteredResults[i];
                    var metadata = result.Document.Metadata;
                    
                    if (_verbosity >= VerbosityLevel.Terse)
                    {
                        System.Console.WriteLine($"{i + 1}. Similarity: {result.Similarity:F3}");
                        
                        // Determine element type and display accordingly
                        if (metadata.ContainsKey("type"))
                        {
                            var type = metadata["type"]?.ToString();
                            if (type == "method_call" && metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                            {
                                System.Console.WriteLine($"   [Method Call] {metadata["caller"]} -> {metadata["callee"]}");
                            }
                            else if (type == "method_definition" && metadata.ContainsKey("method"))
                            {
                                System.Console.WriteLine($"   [Method] {metadata["method"]}");
                            }
                            else if (type == "class_definition" && metadata.ContainsKey("class"))
                            {
                                System.Console.WriteLine($"   [Class] {metadata["class"]}");
                            }
                            else if (type == "property_definition" && metadata.ContainsKey("property"))
                            {
                                System.Console.WriteLine($"   [Property] {metadata["property"]}");
                            }
                            else if (type == "field_definition" && metadata.ContainsKey("field"))
                            {
                                System.Console.WriteLine($"   [Field] {metadata["field"]}");
                            }
                            else if (type == "enum_definition" && metadata.ContainsKey("enum"))
                            {
                                System.Console.WriteLine($"   [Enum] {metadata["enum"]}");
                            }
                            else if (type == "interface_definition" && metadata.ContainsKey("interface"))
                            {
                                System.Console.WriteLine($"   [Interface] {metadata["interface"]}");
                            }
                            else if (type == "struct_definition" && metadata.ContainsKey("struct"))
                            {
                                System.Console.WriteLine($"   [Struct] {metadata["struct"]}");
                            }
                            else
                            {
                                System.Console.WriteLine($"   Content: {result.Document.Content.Substring(0, Math.Min(100, result.Document.Content.Length))}...");
                            }
                        }
                        else
                        {
                            System.Console.WriteLine($"   Content: {result.Document.Content.Substring(0, Math.Min(100, result.Document.Content.Length))}...");
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

    private static async Task ValidateAccuracyAsync(string projectPath)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        if (_analyzer == null)
        {
            System.Console.WriteLine("Analyzer not initialized.");
            return;
        }

        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Validating accuracy for project: {projectPath}");
                System.Console.WriteLine("Re-analyzing source code to get ground truth...");
            }

            // Step 1: Re-analyze the project to get ground truth (without storing)
            var tempAnalyzer = new RoslynAnalyzer(); // No vector store = no storage
            var groundTruth = await tempAnalyzer.AnalyzeProjectAsync(projectPath);

            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Ground truth: {groundTruth.MethodCalls.Count} calls, {groundTruth.MethodDefinitions.Count} methods, {groundTruth.ClassDefinitions.Count} classes");
                System.Console.WriteLine("Loading documents from vector store...");
            }

            // Step 2: Load all documents from vector store
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedCalls = new List<MethodCallInfo>();
            var storedMethodDefs = new List<MethodDefinitionInfo>();
            var storedClassDefs = new List<ClassDefinitionInfo>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (!metadata.ContainsKey("type")) continue;

                var type = metadata["type"].ToString();
                
                if (type == "method_call" && metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var call = new MethodCallInfo
                    {
                        Caller = metadata["caller"]?.ToString() ?? string.Empty,
                        Callee = metadata["callee"]?.ToString() ?? string.Empty,
                        CallerClass = metadata.ContainsKey("caller_class") ? metadata["caller_class"]?.ToString() ?? string.Empty : string.Empty,
                        CalleeClass = metadata.ContainsKey("callee_class") ? metadata["callee_class"]?.ToString() ?? string.Empty : string.Empty,
                        CallerNamespace = metadata.ContainsKey("caller_namespace") ? metadata["caller_namespace"]?.ToString() ?? string.Empty : string.Empty,
                        CalleeNamespace = metadata.ContainsKey("callee_namespace") ? metadata["callee_namespace"]?.ToString() ?? string.Empty : string.Empty,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    storedCalls.Add(call);
                }
                else if (type == "method_definition" && metadata.ContainsKey("method"))
                {
                    var methodDef = new MethodDefinitionInfo
                    {
                        FullyQualifiedName = metadata["method"]?.ToString() ?? string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsVirtual = metadata.ContainsKey("is_virtual") && bool.TryParse(metadata["is_virtual"]?.ToString(), out var isVirtual) && isVirtual,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsOverride = metadata.ContainsKey("is_override") && bool.TryParse(metadata["is_override"]?.ToString(), out var isOverride) && isOverride,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    storedMethodDefs.Add(methodDef);
                }
                else if (type == "class_definition" && metadata.ContainsKey("class"))
                {
                    var classDef = new ClassDefinitionInfo
                    {
                        FullyQualifiedName = metadata["class"]?.ToString() ?? string.Empty,
                        ClassName = metadata.ContainsKey("class_name") ? metadata["class_name"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsSealed = metadata.ContainsKey("is_sealed") && bool.TryParse(metadata["is_sealed"]?.ToString(), out var isSealed) && isSealed,
                        BaseClass = metadata.ContainsKey("base_class") ? metadata["base_class"]?.ToString() ?? string.Empty : string.Empty,
                        Interfaces = metadata.ContainsKey("interfaces") && metadata["interfaces"]?.ToString() is string interfacesStr
                            ? interfacesStr.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList()
                            : new List<string>(),
                        MethodCount = metadata.ContainsKey("method_count") && int.TryParse(metadata["method_count"]?.ToString(), out var methodCount) ? methodCount : 0,
                        PropertyCount = metadata.ContainsKey("property_count") && int.TryParse(metadata["property_count"]?.ToString(), out var propCount) ? propCount : 0,
                        FieldCount = metadata.ContainsKey("field_count") && int.TryParse(metadata["field_count"]?.ToString(), out var fieldCount) ? fieldCount : 0,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    storedClassDefs.Add(classDef);
                }
            }

            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Stored: {storedCalls.Count} calls, {storedMethodDefs.Count} methods, {storedClassDefs.Count} classes");
                System.Console.WriteLine("Comparing...");
            }

            // Step 3: Compare and calculate accuracy
            var callAccuracy = CompareMethodCalls(groundTruth.MethodCalls, storedCalls);
            var methodDefAccuracy = CompareMethodDefinitions(groundTruth.MethodDefinitions, storedMethodDefs);
            var classDefAccuracy = CompareClassDefinitions(groundTruth.ClassDefinitions, storedClassDefs);

            // Step 4: Report results
            System.Console.WriteLine();
            System.Console.WriteLine("=== ACCURACY REPORT ===");
            System.Console.WriteLine();
            
            // Method Calls
            System.Console.WriteLine("METHOD CALLS:");
            System.Console.WriteLine($"  Ground Truth: {groundTruth.MethodCalls.Count}");
            System.Console.WriteLine($"  Stored: {storedCalls.Count}");
            System.Console.WriteLine($"  Correct: {callAccuracy.Correct}");
            System.Console.WriteLine($"  Missing (in code, not in DB): {callAccuracy.Missing}");
            System.Console.WriteLine($"  Extra (in DB, not in code): {callAccuracy.Extra}");
            System.Console.WriteLine($"  Precision: {callAccuracy.Precision:P2} ({callAccuracy.Correct} / {storedCalls.Count})");
            System.Console.WriteLine($"  Recall: {callAccuracy.Recall:P2} ({callAccuracy.Correct} / {groundTruth.MethodCalls.Count})");
            System.Console.WriteLine($"  F1 Score: {callAccuracy.F1Score:P2}");
            
            if (callAccuracy.Missing > 0 && _verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"  Sample missing calls (first 5):");
                foreach (var missing in callAccuracy.MissingItems.Take(5))
                {
                    System.Console.WriteLine($"    - {missing}");
                }
            }
            
            if (callAccuracy.Extra > 0 && _verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"  Sample extra calls (first 5):");
                foreach (var extra in callAccuracy.ExtraItems.Take(5))
                {
                    System.Console.WriteLine($"    - {extra}");
                }
            }

            System.Console.WriteLine();
            
            // Method Definitions
            System.Console.WriteLine("METHOD DEFINITIONS:");
            System.Console.WriteLine($"  Ground Truth: {groundTruth.MethodDefinitions.Count}");
            System.Console.WriteLine($"  Stored: {storedMethodDefs.Count}");
            System.Console.WriteLine($"  Correct: {methodDefAccuracy.Correct}");
            System.Console.WriteLine($"  Missing (in code, not in DB): {methodDefAccuracy.Missing}");
            System.Console.WriteLine($"  Extra (in DB, not in code): {methodDefAccuracy.Extra}");
            System.Console.WriteLine($"  Precision: {methodDefAccuracy.Precision:P2} ({methodDefAccuracy.Correct} / {storedMethodDefs.Count})");
            System.Console.WriteLine($"  Recall: {methodDefAccuracy.Recall:P2} ({methodDefAccuracy.Correct} / {groundTruth.MethodDefinitions.Count})");
            System.Console.WriteLine($"  F1 Score: {methodDefAccuracy.F1Score:P2}");
            
            if (methodDefAccuracy.Missing > 0 && _verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"  Sample missing methods (first 5):");
                foreach (var missing in methodDefAccuracy.MissingItems.Take(5))
                {
                    System.Console.WriteLine($"    - {missing}");
                }
            }
            
            if (methodDefAccuracy.Extra > 0 && _verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"  Sample extra methods (first 5):");
                foreach (var extra in methodDefAccuracy.ExtraItems.Take(5))
                {
                    System.Console.WriteLine($"    - {extra}");
                }
            }

            System.Console.WriteLine();
            
            // Class Definitions
            System.Console.WriteLine("CLASS DEFINITIONS:");
            System.Console.WriteLine($"  Ground Truth: {groundTruth.ClassDefinitions.Count}");
            System.Console.WriteLine($"  Stored: {storedClassDefs.Count}");
            System.Console.WriteLine($"  Correct: {classDefAccuracy.Correct}");
            System.Console.WriteLine($"  Missing (in code, not in DB): {classDefAccuracy.Missing}");
            System.Console.WriteLine($"  Extra (in DB, not in code): {classDefAccuracy.Extra}");
            System.Console.WriteLine($"  Precision: {classDefAccuracy.Precision:P2} ({classDefAccuracy.Correct} / {storedClassDefs.Count})");
            System.Console.WriteLine($"  Recall: {classDefAccuracy.Recall:P2} ({classDefAccuracy.Correct} / {groundTruth.ClassDefinitions.Count})");
            System.Console.WriteLine($"  F1 Score: {classDefAccuracy.F1Score:P2}");
            
            if (classDefAccuracy.Missing > 0 && _verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"  Sample missing classes (first 5):");
                foreach (var missing in classDefAccuracy.MissingItems.Take(5))
                {
                    System.Console.WriteLine($"    - {missing}");
                }
            }
            
            if (classDefAccuracy.Extra > 0 && _verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"  Sample extra classes (first 5):");
                foreach (var extra in classDefAccuracy.ExtraItems.Take(5))
                {
                    System.Console.WriteLine($"    - {extra}");
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine("=== OVERALL ACCURACY ===");
            var totalGroundTruth = groundTruth.MethodCalls.Count + groundTruth.MethodDefinitions.Count + groundTruth.ClassDefinitions.Count;
            var totalStored = storedCalls.Count + storedMethodDefs.Count + storedClassDefs.Count;
            var totalCorrect = callAccuracy.Correct + methodDefAccuracy.Correct + classDefAccuracy.Correct;
            var overallPrecision = totalStored > 0 ? (double)totalCorrect / totalStored : 0.0;
            var overallRecall = totalGroundTruth > 0 ? (double)totalCorrect / totalGroundTruth : 0.0;
            var overallF1 = overallPrecision + overallRecall > 0 ? 2 * overallPrecision * overallRecall / (overallPrecision + overallRecall) : 0.0;
            
            System.Console.WriteLine($"  Overall Precision: {overallPrecision:P2}");
            System.Console.WriteLine($"  Overall Recall: {overallRecall:P2}");
            System.Console.WriteLine($"  Overall F1 Score: {overallF1:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error validating accuracy: {ex.Message}");
            if (_verbosity >= VerbosityLevel.Verbose)
            {
                System.Console.WriteLine(ex.StackTrace);
            }
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }

    private static AccuracyMetrics CompareMethodCalls(List<MethodCallInfo> groundTruth, List<MethodCallInfo> stored)
    {
        var groundTruthSet = new HashSet<string>(groundTruth.Select(c => $"{c.Caller}->{c.Callee}@{c.FilePath}:{c.LineNumber}"));
        var storedSet = new HashSet<string>(stored.Select(c => $"{c.Caller}->{c.Callee}@{c.FilePath}:{c.LineNumber}"));
        
        var correct = groundTruthSet.Intersect(storedSet).Count();
        var missing = groundTruthSet.Except(storedSet).ToList();
        var extra = storedSet.Except(groundTruthSet).ToList();
        
        var precision = stored.Count > 0 ? (double)correct / stored.Count : 0.0;
        var recall = groundTruth.Count > 0 ? (double)correct / groundTruth.Count : 0.0;
        var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0.0;
        
        return new AccuracyMetrics
        {
            Correct = correct,
            Missing = missing.Count,
            Extra = extra.Count,
            Precision = precision,
            Recall = recall,
            F1Score = f1,
            MissingItems = missing,
            ExtraItems = extra
        };
    }

    private static AccuracyMetrics CompareMethodDefinitions(List<MethodDefinitionInfo> groundTruth, List<MethodDefinitionInfo> stored)
    {
        var groundTruthSet = new HashSet<string>(groundTruth.Select(m => m.FullyQualifiedName));
        var storedSet = new HashSet<string>(stored.Select(m => m.FullyQualifiedName));
        
        var correct = groundTruthSet.Intersect(storedSet).Count();
        var missing = groundTruthSet.Except(storedSet).ToList();
        var extra = storedSet.Except(groundTruthSet).ToList();
        
        var precision = stored.Count > 0 ? (double)correct / stored.Count : 0.0;
        var recall = groundTruth.Count > 0 ? (double)correct / groundTruth.Count : 0.0;
        var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0.0;
        
        return new AccuracyMetrics
        {
            Correct = correct,
            Missing = missing.Count,
            Extra = extra.Count,
            Precision = precision,
            Recall = recall,
            F1Score = f1,
            MissingItems = missing,
            ExtraItems = extra
        };
    }

    private static AccuracyMetrics CompareClassDefinitions(List<ClassDefinitionInfo> groundTruth, List<ClassDefinitionInfo> stored)
    {
        var groundTruthSet = new HashSet<string>(groundTruth.Select(c => c.FullyQualifiedName));
        var storedSet = new HashSet<string>(stored.Select(c => c.FullyQualifiedName));
        
        var correct = groundTruthSet.Intersect(storedSet).Count();
        var missing = groundTruthSet.Except(storedSet).ToList();
        var extra = storedSet.Except(groundTruthSet).ToList();
        
        var precision = stored.Count > 0 ? (double)correct / stored.Count : 0.0;
        var recall = groundTruth.Count > 0 ? (double)correct / groundTruth.Count : 0.0;
        var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0.0;
        
        return new AccuracyMetrics
        {
            Correct = correct,
            Missing = missing.Count,
            Extra = extra.Count,
            Precision = precision,
            Recall = recall,
            F1Score = f1,
            MissingItems = missing,
            ExtraItems = extra
        };
    }

    private static async Task CleanupStaleDataAsync(string projectPath)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        if (_analyzer == null)
        {
            System.Console.WriteLine("Analyzer not initialized.");
            return;
        }

        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Cleaning up stale data for project: {projectPath}");
                System.Console.WriteLine("Analyzing current codebase to identify valid items...");
            }

            // Step 1: Get ground truth from current code
            var tempAnalyzer = new RoslynAnalyzer();
            var groundTruth = await tempAnalyzer.AnalyzeProjectAsync(projectPath);

            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Current codebase: {groundTruth.MethodCalls.Count} calls, {groundTruth.MethodDefinitions.Count} methods, {groundTruth.ClassDefinitions.Count} classes");
                System.Console.WriteLine("Loading documents from vector store...");
            }

            // Step 2: Build sets of valid items
            var validCallKeys = new HashSet<string>(groundTruth.MethodCalls.Select(c => $"{c.Caller}->{c.Callee}@{c.FilePath}:{c.LineNumber}"));
            var validMethodKeys = new HashSet<string>(groundTruth.MethodDefinitions.Select(m => m.FullyQualifiedName));
            var validClassKeys = new HashSet<string>(groundTruth.ClassDefinitions.Select(c => c.FullyQualifiedName));

            // Step 3: Load all documents and identify stale ones
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var staleIds = new List<string>();
            var keptCounts = new Dictionary<string, int> { ["calls"] = 0, ["methods"] = 0, ["classes"] = 0 };
            var deletedCounts = new Dictionary<string, int> { ["calls"] = 0, ["methods"] = 0, ["classes"] = 0 };

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (!metadata.ContainsKey("type")) continue;

                var type = metadata["type"].ToString();
                bool isStale = false;

                if (type == "method_call" && metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var caller = metadata["caller"]?.ToString() ?? string.Empty;
                    var callee = metadata["callee"]?.ToString() ?? string.Empty;
                    var filePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty;
                    var lineNumber = metadata.ContainsKey("line_number") ? metadata["line_number"]?.ToString() ?? "0" : "0";
                    var key = $"{caller}->{callee}@{filePath}:{lineNumber}";
                    
                    if (validCallKeys.Contains(key))
                    {
                        keptCounts["calls"]++;
                    }
                    else
                    {
                        isStale = true;
                        deletedCounts["calls"]++;
                    }
                }
                else if (type == "method_definition" && metadata.ContainsKey("method"))
                {
                    var methodKey = metadata["method"]?.ToString() ?? string.Empty;
                    if (validMethodKeys.Contains(methodKey))
                    {
                        keptCounts["methods"]++;
                    }
                    else
                    {
                        isStale = true;
                        deletedCounts["methods"]++;
                    }
                }
                else if (type == "class_definition" && metadata.ContainsKey("class"))
                {
                    var classKey = metadata["class"]?.ToString() ?? string.Empty;
                    if (validClassKeys.Contains(classKey))
                    {
                        keptCounts["classes"]++;
                    }
                    else
                    {
                        isStale = true;
                        deletedCounts["classes"]++;
                    }
                }

                if (isStale)
                {
                    staleIds.Add(id);
                }
            }

            // Step 4: Delete stale items
            if (staleIds.Count > 0)
            {
                if (_verbosity >= VerbosityLevel.Normal)
                {
                    System.Console.WriteLine($"Found {staleIds.Count} stale items to remove:");
                    System.Console.WriteLine($"  Method calls: {deletedCounts["calls"]} to delete, {keptCounts["calls"]} to keep");
                    System.Console.WriteLine($"  Method definitions: {deletedCounts["methods"]} to delete, {keptCounts["methods"]} to keep");
                    System.Console.WriteLine($"  Class definitions: {deletedCounts["classes"]} to delete, {keptCounts["classes"]} to keep");
                    System.Console.WriteLine("Deleting stale items...");
                }

                var deleted = 0;
                foreach (var id in staleIds)
                {
                    var success = await _vectorStore.Store.DeleteAsync(id);
                    if (success) deleted++;
                }

                if (_verbosity >= VerbosityLevel.Terse)
                {
                    System.Console.WriteLine($"Cleanup complete: Deleted {deleted} stale items.");
                }

                if (_verbosity >= VerbosityLevel.Normal)
                {
                    System.Console.WriteLine($"Successfully deleted {deleted} out of {staleIds.Count} stale items.");
                    System.Console.WriteLine($"Remaining items: {keptCounts["calls"]} calls, {keptCounts["methods"]} methods, {keptCounts["classes"]} classes");
                }
            }
            else
            {
                if (_verbosity >= VerbosityLevel.Terse)
                {
                    System.Console.WriteLine("No stale data found. Vector store is clean.");
                }

                if (_verbosity >= VerbosityLevel.Normal)
                {
                    System.Console.WriteLine("No stale items found. All stored items match the current codebase.");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error cleaning up stale data: {ex.Message}");
            if (_verbosity >= VerbosityLevel.Verbose)
            {
                System.Console.WriteLine(ex.StackTrace);
            }
        }
        
        if (_verbosity >= VerbosityLevel.Normal)
        {
            System.Console.WriteLine();
        }
    }

    // API-equivalent command implementations
    private static async Task ListClassesAsync(string[] args)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var classes = new List<ClassDefinitionInfo>();
            string? namespaceFilter = null;
            int limit = 100;
            int offset = 0;

            // Parse optional arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--namespace" && i + 1 < args.Length)
                    namespaceFilter = args[i + 1];
                else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[i + 1], out var l))
                    limit = l;
                else if (args[i] == "--offset" && i + 1 < args.Length && int.TryParse(args[i + 1], out var o))
                    offset = o;
            }

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" && metadata.ContainsKey("class"))
                {
                    var classDef = new ClassDefinitionInfo
                    {
                        FullyQualifiedName = metadata["class"]?.ToString() ?? string.Empty,
                        ClassName = metadata.ContainsKey("class_name") ? metadata["class_name"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsSealed = metadata.ContainsKey("is_sealed") && bool.TryParse(metadata["is_sealed"]?.ToString(), out var isSealed) && isSealed,
                        BaseClass = metadata.ContainsKey("base_class") ? metadata["base_class"]?.ToString() ?? string.Empty : string.Empty,
                        Interfaces = metadata.ContainsKey("interfaces") && metadata["interfaces"]?.ToString() is string interfacesStr
                            ? interfacesStr.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList()
                            : new List<string>(),
                        MethodCount = metadata.ContainsKey("method_count") && int.TryParse(metadata["method_count"]?.ToString(), out var methodCount) ? methodCount : 0,
                        PropertyCount = metadata.ContainsKey("property_count") && int.TryParse(metadata["property_count"]?.ToString(), out var propCount) ? propCount : 0,
                        FieldCount = metadata.ContainsKey("field_count") && int.TryParse(metadata["field_count"]?.ToString(), out var fieldCount) ? fieldCount : 0,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };

                    if (namespaceFilter == null || classDef.Namespace.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        classes.Add(classDef);
                    }
                }
            }

            var totalCount = classes.Count;
            var paginated = classes.Skip(offset).Take(limit).ToList();

            System.Console.WriteLine($"Classes (showing {paginated.Count} of {totalCount}):");
            foreach (var cls in paginated)
            {
                System.Console.WriteLine($"  {cls.FullyQualifiedName}");
                System.Console.WriteLine($"    Namespace: {cls.Namespace}");
                System.Console.WriteLine($"    File: {cls.FilePath}:{cls.LineNumber}");
                System.Console.WriteLine($"    Members: {cls.MethodCount} methods, {cls.PropertyCount} properties, {cls.FieldCount} fields");
            }
            if (totalCount > offset + limit)
            {
                System.Console.WriteLine($"  ... and {totalCount - offset - limit} more (use --offset {offset + limit} to see more)");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error listing classes: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task ListMethodsAsync(string[] args)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var methods = new List<MethodDefinitionInfo>();
            string? classNameFilter = null;
            string? namespaceFilter = null;
            int limit = 100;
            int offset = 0;

            // Parse optional arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--class" && i + 1 < args.Length)
                    classNameFilter = args[i + 1];
                else if (args[i] == "--namespace" && i + 1 < args.Length)
                    namespaceFilter = args[i + 1];
                else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[i + 1], out var l))
                    limit = l;
                else if (args[i] == "--offset" && i + 1 < args.Length && int.TryParse(args[i + 1], out var o))
                    offset = o;
            }

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && metadata.ContainsKey("method"))
                {
                    var methodDef = new MethodDefinitionInfo
                    {
                        FullyQualifiedName = metadata["method"]?.ToString() ?? string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsVirtual = metadata.ContainsKey("is_virtual") && bool.TryParse(metadata["is_virtual"]?.ToString(), out var isVirtual) && isVirtual,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsOverride = metadata.ContainsKey("is_override") && bool.TryParse(metadata["is_override"]?.ToString(), out var isOverride) && isOverride,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };

                    bool matches = true;
                    if (classNameFilter != null && !methodDef.ClassName.Contains(classNameFilter, StringComparison.OrdinalIgnoreCase))
                        matches = false;
                    if (namespaceFilter != null && !methodDef.Namespace.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase))
                        matches = false;

                    if (matches)
                    {
                        methods.Add(methodDef);
                    }
                }
            }

            var totalCount = methods.Count;
            var paginated = methods.Skip(offset).Take(limit).ToList();

            System.Console.WriteLine($"Methods (showing {paginated.Count} of {totalCount}):");
            foreach (var method in paginated)
            {
                var paramsStr = string.Join(", ", method.Parameters);
                System.Console.WriteLine($"  {method.FullyQualifiedName}");
                System.Console.WriteLine($"    {method.AccessModifier} {method.ReturnType} {method.MethodName}({paramsStr})");
                System.Console.WriteLine($"    File: {method.FilePath}:{method.LineNumber}");
            }
            if (totalCount > offset + limit)
            {
                System.Console.WriteLine($"  ... and {totalCount - offset - limit} more (use --offset {offset + limit} to see more)");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error listing methods: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task ListEntryPointsAsync(string[] args)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            string? typeFilter = null;
            
            // Parse optional arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--type" && i + 1 < args.Length)
                {
                    typeFilter = args[i + 1].ToLowerInvariant();
                }
            }

            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var entryPoints = new List<(MethodDefinitionInfo method, string type, string? route, string? httpMethod)>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && metadata.ContainsKey("method"))
                {
                    var methodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty;
                    var className = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;

                    string? entryType = null;
                    string? route = null;
                    string? httpMethod = null;

                    // Identify entry points: Main methods, or methods in Controllers
                    if (methodName == "Main")
                    {
                        entryType = "main";
                    }
                    else if (className.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                    {
                        entryType = "controller";
                        // Try to detect route and HTTP method from method name or metadata
                        // This is a simplified detection - full implementation would require analyzing attributes
                        if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                            httpMethod = "GET";
                        else if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase))
                            httpMethod = "POST";
                        else if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase))
                            httpMethod = "PUT";
                        else if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
                            httpMethod = "DELETE";
                        
                        // Route detection would require analyzing [Route] attributes - not available in current metadata
                        // For now, we'll leave it as null
                    }

                    if (entryType != null)
                    {
                        // Apply type filter if specified
                        if (typeFilter != null && typeFilter != "all")
                        {
                            if (typeFilter == "main" && entryType != "main") continue;
                            if (typeFilter == "controller" && entryType != "controller") continue;
                            if (typeFilter == "api" && entryType != "controller") continue;
                        }

                        var methodDef = new MethodDefinitionInfo
                        {
                            FullyQualifiedName = metadata["method"]?.ToString() ?? string.Empty,
                            MethodName = methodName,
                            ClassName = className,
                            Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                            ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                            Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                                ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                                : new List<string>(),
                            AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                            IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                            FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                            LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                        };
                        entryPoints.Add((methodDef, entryType, route, httpMethod));
                    }
                }
            }

            System.Console.WriteLine($"Entry Points ({entryPoints.Count}):");
            foreach (var (ep, epType, epRoute, epHttpMethod) in entryPoints)
            {
                System.Console.WriteLine($"  [{epType}] {ep.FullyQualifiedName}");
                System.Console.WriteLine($"    File: {ep.FilePath}:{ep.LineNumber}");
                if (epRoute != null)
                {
                    System.Console.WriteLine($"    Route: {epRoute}");
                }
                if (epHttpMethod != null)
                {
                    System.Console.WriteLine($"    HTTP Method: {epHttpMethod}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error listing entry points: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task GetMethodAsync(string methodFqn)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            MethodDefinitionInfo? found = null;

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && 
                    metadata.ContainsKey("method") && metadata["method"]?.ToString() == methodFqn)
                {
                    found = new MethodDefinitionInfo
                    {
                        FullyQualifiedName = metadata["method"]?.ToString() ?? string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsVirtual = metadata.ContainsKey("is_virtual") && bool.TryParse(metadata["is_virtual"]?.ToString(), out var isVirtual) && isVirtual,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsOverride = metadata.ContainsKey("is_override") && bool.TryParse(metadata["is_override"]?.ToString(), out var isOverride) && isOverride,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    break;
                }
            }

            if (found == null)
            {
                System.Console.WriteLine($"Method not found: {methodFqn}");
                return;
            }

            var methodParamsStr = string.Join(", ", found.Parameters);
            var modifiers = new List<string>();
            if (found.IsStatic) modifiers.Add("static");
            if (found.IsVirtual) modifiers.Add("virtual");
            if (found.IsAbstract) modifiers.Add("abstract");
            if (found.IsOverride) modifiers.Add("override");
            var modifierStr = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";

            System.Console.WriteLine($"Method: {found.FullyQualifiedName}");
            System.Console.WriteLine($"  Signature: {found.AccessModifier} {modifierStr}{found.ReturnType} {found.MethodName}({methodParamsStr})");
            System.Console.WriteLine($"  Class: {found.ClassName}");
            System.Console.WriteLine($"  Namespace: {found.Namespace}");
            System.Console.WriteLine($"  File: {found.FilePath}:{found.LineNumber}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting method: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task GetClassAsync(string classFqn)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            ClassDefinitionInfo? found = null;

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" && 
                    metadata.ContainsKey("class") && metadata["class"]?.ToString() == classFqn)
                {
                    found = new ClassDefinitionInfo
                    {
                        FullyQualifiedName = metadata["class"]?.ToString() ?? string.Empty,
                        ClassName = metadata.ContainsKey("class_name") ? metadata["class_name"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsSealed = metadata.ContainsKey("is_sealed") && bool.TryParse(metadata["is_sealed"]?.ToString(), out var isSealed) && isSealed,
                        BaseClass = metadata.ContainsKey("base_class") ? metadata["base_class"]?.ToString() ?? string.Empty : string.Empty,
                        Interfaces = metadata.ContainsKey("interfaces") && metadata["interfaces"]?.ToString() is string interfacesStr
                            ? interfacesStr.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList()
                            : new List<string>(),
                        MethodCount = metadata.ContainsKey("method_count") && int.TryParse(metadata["method_count"]?.ToString(), out var methodCount) ? methodCount : 0,
                        PropertyCount = metadata.ContainsKey("property_count") && int.TryParse(metadata["property_count"]?.ToString(), out var propCount) ? propCount : 0,
                        FieldCount = metadata.ContainsKey("field_count") && int.TryParse(metadata["field_count"]?.ToString(), out var fieldCount) ? fieldCount : 0,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    break;
                }
            }

            if (found == null)
            {
                System.Console.WriteLine($"Class not found: {classFqn}");
                return;
            }

            var modifiers = new List<string>();
            if (found.IsStatic) modifiers.Add("static");
            if (found.IsAbstract) modifiers.Add("abstract");
            if (found.IsSealed) modifiers.Add("sealed");
            var modifierStr = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";

            var inheritance = "";
            if (!string.IsNullOrEmpty(found.BaseClass))
                inheritance = $" : {found.BaseClass}";
            if (found.Interfaces.Count > 0)
                inheritance += (string.IsNullOrEmpty(found.BaseClass) ? " : " : ", ") + string.Join(", ", found.Interfaces);

            System.Console.WriteLine($"Class: {found.FullyQualifiedName}");
            System.Console.WriteLine($"  Declaration: {found.AccessModifier} {modifierStr}{found.ClassName}{inheritance}");
            System.Console.WriteLine($"  Namespace: {found.Namespace}");
            System.Console.WriteLine($"  Members: {found.MethodCount} methods, {found.PropertyCount} properties, {found.FieldCount} fields");
            System.Console.WriteLine($"  File: {found.FilePath}:{found.LineNumber}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting class: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task GetCallersAsync(string methodFqn, int depth)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var callers = new List<(string caller, string filePath, int lineNumber, int depth)>();

            // Build a map of callee -> callers for efficient lookup
            var callerMap = new Dictionary<string, HashSet<(string caller, string filePath, int lineNumber)>>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                    metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var caller = metadata["caller"]?.ToString() ?? string.Empty;
                    var callee = metadata["callee"]?.ToString() ?? string.Empty;
                    var filePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty;
                    var lineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0;

                    if (!callerMap.ContainsKey(callee))
                        callerMap[callee] = new HashSet<(string, string, int)>();
                    callerMap[callee].Add((caller, filePath, lineNumber));
                }
            }

            // Recursive traversal up the call tree
            var visited = new HashSet<string>();
            var queue = new Queue<(string method, int currentDepth)>();
            queue.Enqueue((methodFqn, 0));

            while (queue.Count > 0)
            {
                var (currentMethod, currentDepth) = queue.Dequeue();
                if (currentDepth >= depth || visited.Contains(currentMethod))
                    continue;

                visited.Add(currentMethod);

                if (callerMap.ContainsKey(currentMethod))
                {
                    foreach (var (caller, filePath, lineNumber) in callerMap[currentMethod])
                    {
                        callers.Add((caller, filePath, lineNumber, currentDepth + 1));
                        if (currentDepth + 1 < depth)
                        {
                            queue.Enqueue((caller, currentDepth + 1));
                        }
                    }
                }
            }

            System.Console.WriteLine($"Callers of {methodFqn} (depth {depth}):");
            if (callers.Count == 0)
            {
                System.Console.WriteLine("  No callers found.");
            }
            else
            {
                foreach (var (caller, filePath, lineNumber, callDepth) in callers.OrderBy(c => c.depth).ThenBy(c => c.caller))
                {
                    System.Console.WriteLine($"  [{callDepth}] {caller}");
                    System.Console.WriteLine($"      File: {filePath}:{lineNumber}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting callers: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task GetCalleesAsync(string methodFqn, int depth)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var callees = new List<(string callee, string filePath, int lineNumber, int depth)>();

            // Build a map of caller -> callees for efficient lookup
            var calleeMap = new Dictionary<string, HashSet<(string callee, string filePath, int lineNumber)>>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                    metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var caller = metadata["caller"]?.ToString() ?? string.Empty;
                    var callee = metadata["callee"]?.ToString() ?? string.Empty;
                    var filePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty;
                    var lineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0;

                    if (!calleeMap.ContainsKey(caller))
                        calleeMap[caller] = new HashSet<(string, string, int)>();
                    calleeMap[caller].Add((callee, filePath, lineNumber));
                }
            }

            // Recursive traversal down the call tree
            var visited = new HashSet<string>();
            var queue = new Queue<(string method, int currentDepth)>();
            queue.Enqueue((methodFqn, 0));

            while (queue.Count > 0)
            {
                var (currentMethod, currentDepth) = queue.Dequeue();
                if (currentDepth >= depth || visited.Contains(currentMethod))
                    continue;

                visited.Add(currentMethod);

                if (calleeMap.ContainsKey(currentMethod))
                {
                    foreach (var (callee, filePath, lineNumber) in calleeMap[currentMethod])
                    {
                        callees.Add((callee, filePath, lineNumber, currentDepth + 1));
                        if (currentDepth + 1 < depth)
                        {
                            queue.Enqueue((callee, currentDepth + 1));
                        }
                    }
                }
            }

            System.Console.WriteLine($"Callees of {methodFqn} (depth {depth}):");
            if (callees.Count == 0)
            {
                System.Console.WriteLine("  No callees found.");
            }
            else
            {
                foreach (var (callee, filePath, lineNumber, callDepth) in callees.OrderBy(c => c.depth).ThenBy(c => c.callee))
                {
                    System.Console.WriteLine($"  [{callDepth}] {callee}");
                    System.Console.WriteLine($"      File: {filePath}:{lineNumber}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting callees: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task GetClassMethodsAsync(string classFqn)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var methods = new List<MethodDefinitionInfo>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && 
                    metadata.ContainsKey("class") && metadata["class"]?.ToString() == classFqn)
                {
                    var methodDef = new MethodDefinitionInfo
                    {
                        FullyQualifiedName = metadata["method"]?.ToString() ?? string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsVirtual = metadata.ContainsKey("is_virtual") && bool.TryParse(metadata["is_virtual"]?.ToString(), out var isVirtual) && isVirtual,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsOverride = metadata.ContainsKey("is_override") && bool.TryParse(metadata["is_override"]?.ToString(), out var isOverride) && isOverride,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    methods.Add(methodDef);
                }
            }

            System.Console.WriteLine($"Methods in {classFqn} ({methods.Count}):");
            foreach (var method in methods.OrderBy(m => m.LineNumber))
            {
                var paramsStr = string.Join(", ", method.Parameters);
                var modifiers = new List<string>();
                if (method.IsStatic) modifiers.Add("static");
                if (method.IsVirtual) modifiers.Add("virtual");
                if (method.IsAbstract) modifiers.Add("abstract");
                if (method.IsOverride) modifiers.Add("override");
                var modifierStr = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";

                System.Console.WriteLine($"  {method.AccessModifier} {modifierStr}{method.ReturnType} {method.MethodName}({paramsStr})");
                System.Console.WriteLine($"    Line: {method.FilePath}:{method.LineNumber}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting class methods: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task GetClassReferencesAsync(string classFqn)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var references = new Dictionary<string, (string relationship, List<string> methodsCalled)>();

            // Extract class name from FQN
            var className = classFqn.Split('.').LastOrDefault() ?? classFqn;

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                
                // Check for method calls where callee is in this class
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                    metadata.ContainsKey("callee_class") && metadata.ContainsKey("caller_class"))
                {
                    var calleeClass = metadata["callee_class"]?.ToString() ?? string.Empty;
                    var callerClass = metadata.ContainsKey("caller_class") ? metadata["caller_class"]?.ToString() ?? string.Empty : string.Empty;
                    var callee = metadata.ContainsKey("callee") ? metadata["callee"]?.ToString() ?? string.Empty : string.Empty;

                    if (calleeClass == className || callee.Contains(classFqn))
                    {
                        if (!references.ContainsKey(callerClass))
                            references[callerClass] = ("calls_methods", new List<string>());
                        references[callerClass].methodsCalled.Add(callee);
                    }
                }

                // Check for inheritance relationships
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" &&
                    metadata.ContainsKey("base_class"))
                {
                    var baseClass = metadata["base_class"]?.ToString() ?? string.Empty;
                    var thisClass = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;
                    var thisClassName = thisClass.Split('.').LastOrDefault() ?? thisClass;

                    if (baseClass == classFqn || baseClass.Contains(className))
                    {
                        if (!references.ContainsKey(thisClass))
                            references[thisClass] = ("inherits", new List<string>());
                    }
                }
            }

            System.Console.WriteLine($"Classes referencing {classFqn} ({references.Count}):");
            if (references.Count == 0)
            {
                System.Console.WriteLine("  No references found.");
            }
            else
            {
                foreach (var kvp in references.OrderBy(r => r.Key))
                {
                    System.Console.WriteLine($"  {kvp.Key} [{kvp.Value.relationship}]");
                    if (kvp.Value.methodsCalled.Count > 0)
                    {
                        foreach (var method in kvp.Value.methodsCalled.Take(5))
                        {
                            System.Console.WriteLine($"    - {method}");
                        }
                        if (kvp.Value.methodsCalled.Count > 5)
                        {
                            System.Console.WriteLine($"    ... and {kvp.Value.methodsCalled.Count - 5} more");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting class references: {ex.Message}");
        }
        System.Console.WriteLine();
    }

    private static async Task DiffProjectAsync(string[] args)
    {
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        if (args.Length == 0)
        {
            System.Console.WriteLine("Usage: diff-project <project-path> [--types <types>] [--include-details]");
            return;
        }

        var projectPath = args[0];
        var types = new HashSet<string> { "method_calls", "method_definitions", "class_definitions" };
        bool includeDetails = true;

        // Parse optional arguments
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--types" && i + 1 < args.Length)
            {
                types.Clear();
                i++;
                while (i < args.Length && !args[i].StartsWith("--"))
                {
                    var type = args[i].ToLowerInvariant();
                    if (type == "method_calls" || type == "method_definitions" || type == "class_definitions")
                    {
                        types.Add(type);
                    }
                    i++;
                }
                i--; // Adjust for loop increment
            }
            else if (args[i] == "--include-details")
            {
                includeDetails = true;
            }
            else if (args[i] == "--no-details")
            {
                includeDetails = false;
            }
        }

        try
        {
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Comparing codebase with database for: {projectPath}");
                System.Console.WriteLine("Re-analyzing source code to get ground truth...");
            }

            // Get ground truth from current code
            var tempAnalyzer = new RoslynAnalyzer();
            var groundTruth = await tempAnalyzer.AnalyzeProjectAsync(projectPath);

            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Current codebase: {groundTruth.MethodCalls.Count} calls, {groundTruth.MethodDefinitions.Count} methods, {groundTruth.ClassDefinitions.Count} classes");
                System.Console.WriteLine("Loading documents from vector store...");
            }

            // Load stored data
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedCalls = new List<MethodCallInfo>();
            var storedMethodDefs = new List<MethodDefinitionInfo>();
            var storedClassDefs = new List<ClassDefinitionInfo>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (!metadata.ContainsKey("type")) continue;

                var type = metadata["type"].ToString();
                
                if (type == "method_call" && metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var call = new MethodCallInfo
                    {
                        Caller = metadata["caller"]?.ToString() ?? string.Empty,
                        Callee = metadata["callee"]?.ToString() ?? string.Empty,
                        CallerClass = metadata.ContainsKey("caller_class") ? metadata["caller_class"]?.ToString() ?? string.Empty : string.Empty,
                        CalleeClass = metadata.ContainsKey("callee_class") ? metadata["callee_class"]?.ToString() ?? string.Empty : string.Empty,
                        CallerNamespace = metadata.ContainsKey("caller_namespace") ? metadata["caller_namespace"]?.ToString() ?? string.Empty : string.Empty,
                        CalleeNamespace = metadata.ContainsKey("callee_namespace") ? metadata["callee_namespace"]?.ToString() ?? string.Empty : string.Empty,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    storedCalls.Add(call);
                }
                else if (type == "method_definition" && metadata.ContainsKey("method"))
                {
                    var methodDef = new MethodDefinitionInfo
                    {
                        FullyQualifiedName = metadata["method"]?.ToString() ?? string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsVirtual = metadata.ContainsKey("is_virtual") && bool.TryParse(metadata["is_virtual"]?.ToString(), out var isVirtual) && isVirtual,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsOverride = metadata.ContainsKey("is_override") && bool.TryParse(metadata["is_override"]?.ToString(), out var isOverride) && isOverride,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    storedMethodDefs.Add(methodDef);
                }
                else if (type == "class_definition" && metadata.ContainsKey("class"))
                {
                    var classDef = new ClassDefinitionInfo
                    {
                        FullyQualifiedName = metadata["class"]?.ToString() ?? string.Empty,
                        ClassName = metadata.ContainsKey("class_name") ? metadata["class_name"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsSealed = metadata.ContainsKey("is_sealed") && bool.TryParse(metadata["is_sealed"]?.ToString(), out var isSealed) && isSealed,
                        BaseClass = metadata.ContainsKey("base_class") ? metadata["base_class"]?.ToString() ?? string.Empty : string.Empty,
                        Interfaces = metadata.ContainsKey("interfaces") && metadata["interfaces"]?.ToString() is string interfacesStr
                            ? interfacesStr.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList()
                            : new List<string>(),
                        MethodCount = metadata.ContainsKey("method_count") && int.TryParse(metadata["method_count"]?.ToString(), out var methodCount) ? methodCount : 0,
                        PropertyCount = metadata.ContainsKey("property_count") && int.TryParse(metadata["property_count"]?.ToString(), out var propCount) ? propCount : 0,
                        FieldCount = metadata.ContainsKey("field_count") && int.TryParse(metadata["field_count"]?.ToString(), out var fieldCount) ? fieldCount : 0,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var line) ? line : 0
                    };
                    storedClassDefs.Add(classDef);
                }
            }

            // Compare and report
            System.Console.WriteLine();
            System.Console.WriteLine("=== PROJECT DIFF ===");
            System.Console.WriteLine();

            if (types.Contains("method_calls"))
            {
                var callDiff = CompareMethodCalls(groundTruth.MethodCalls, storedCalls);
                System.Console.WriteLine("METHOD CALLS:");
                System.Console.WriteLine($"  In code (not in DB): {callDiff.Missing}");
                System.Console.WriteLine($"  In DB (not in code): {callDiff.Extra}");
                System.Console.WriteLine($"  Total in code: {groundTruth.MethodCalls.Count}");
                System.Console.WriteLine($"  Total in DB: {storedCalls.Count}");
                
                if (includeDetails && callDiff.Missing > 0)
                {
                    System.Console.WriteLine($"  Missing items (first 10):");
                    foreach (var missing in callDiff.MissingItems.Take(10))
                    {
                        System.Console.WriteLine($"    - {missing}");
                    }
                    if (callDiff.Missing > 10)
                    {
                        System.Console.WriteLine($"    ... and {callDiff.Missing - 10} more");
                    }
                }
                
                if (includeDetails && callDiff.Extra > 0)
                {
                    System.Console.WriteLine($"  Extra items (first 10):");
                    foreach (var extra in callDiff.ExtraItems.Take(10))
                    {
                        System.Console.WriteLine($"    - {extra}");
                    }
                    if (callDiff.Extra > 10)
                    {
                        System.Console.WriteLine($"    ... and {callDiff.Extra - 10} more");
                    }
                }
                System.Console.WriteLine();
            }

            if (types.Contains("method_definitions"))
            {
                var methodDiff = CompareMethodDefinitions(groundTruth.MethodDefinitions, storedMethodDefs);
                System.Console.WriteLine("METHOD DEFINITIONS:");
                System.Console.WriteLine($"  In code (not in DB): {methodDiff.Missing}");
                System.Console.WriteLine($"  In DB (not in code): {methodDiff.Extra}");
                System.Console.WriteLine($"  Total in code: {groundTruth.MethodDefinitions.Count}");
                System.Console.WriteLine($"  Total in DB: {storedMethodDefs.Count}");
                
                if (includeDetails && methodDiff.Missing > 0)
                {
                    System.Console.WriteLine($"  Missing items (first 10):");
                    foreach (var missing in methodDiff.MissingItems.Take(10))
                    {
                        System.Console.WriteLine($"    - {missing}");
                    }
                    if (methodDiff.Missing > 10)
                    {
                        System.Console.WriteLine($"    ... and {methodDiff.Missing - 10} more");
                    }
                }
                
                if (includeDetails && methodDiff.Extra > 0)
                {
                    System.Console.WriteLine($"  Extra items (first 10):");
                    foreach (var extra in methodDiff.ExtraItems.Take(10))
                    {
                        System.Console.WriteLine($"    - {extra}");
                    }
                    if (methodDiff.Extra > 10)
                    {
                        System.Console.WriteLine($"    ... and {methodDiff.Extra - 10} more");
                    }
                }
                System.Console.WriteLine();
            }

            if (types.Contains("class_definitions"))
            {
                var classDiff = CompareClassDefinitions(groundTruth.ClassDefinitions, storedClassDefs);
                System.Console.WriteLine("CLASS DEFINITIONS:");
                System.Console.WriteLine($"  In code (not in DB): {classDiff.Missing}");
                System.Console.WriteLine($"  In DB (not in code): {classDiff.Extra}");
                System.Console.WriteLine($"  Total in code: {groundTruth.ClassDefinitions.Count}");
                System.Console.WriteLine($"  Total in DB: {storedClassDefs.Count}");
                
                if (includeDetails && classDiff.Missing > 0)
                {
                    System.Console.WriteLine($"  Missing items (first 10):");
                    foreach (var missing in classDiff.MissingItems.Take(10))
                    {
                        System.Console.WriteLine($"    - {missing}");
                    }
                    if (classDiff.Missing > 10)
                    {
                        System.Console.WriteLine($"    ... and {classDiff.Missing - 10} more");
                    }
                }
                
                if (includeDetails && classDiff.Extra > 0)
                {
                    System.Console.WriteLine($"  Extra items (first 10):");
                    foreach (var extra in classDiff.ExtraItems.Take(10))
                    {
                        System.Console.WriteLine($"    - {extra}");
                    }
                    if (classDiff.Extra > 10)
                    {
                        System.Console.WriteLine($"    ... and {classDiff.Extra - 10} more");
                    }
                }
                System.Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error comparing project: {ex.Message}");
            if (_verbosity >= VerbosityLevel.Verbose)
            {
                System.Console.WriteLine(ex.StackTrace);
            }
        }
        System.Console.WriteLine();
    }

    private static async Task ListProjectsAsync()
    {
        // NOTE: This is a simplified implementation. The API spec assumes a project management system
        // with multiple projects and project IDs. The current codebase uses a single vector store.
        // This command shows information about the current vector store as a "project".
        
        if (_vectorStore == null)
        {
            System.Console.WriteLine("Vector store not initialized.");
            return;
        }

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            
            // Count different types
            var methodCallCount = 0;
            var methodDefCount = 0;
            var classDefCount = 0;
            var lastModified = DateTime.MinValue;

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (!metadata.ContainsKey("type")) continue;

                var type = metadata["type"]?.ToString();
                if (type == "method_call") methodCallCount++;
                else if (type == "method_definition") methodDefCount++;
                else if (type == "class_definition") classDefCount++;

                // Try to get last modified time if available
                var docPath = System.IO.Path.Combine(_storePath, "documents", $"{id}.json");
                if (System.IO.File.Exists(docPath))
                {
                    var fileInfo = new System.IO.FileInfo(docPath);
                    if (fileInfo.LastWriteTime > lastModified)
                        lastModified = fileInfo.LastWriteTime;
                }
            }

            System.Console.WriteLine("Projects (current vector store):");
            System.Console.WriteLine($"  Project ID: current-store");
            System.Console.WriteLine($"  Name: {System.IO.Path.GetFileName(System.IO.Path.GetFullPath(_storePath))}");
            System.Console.WriteLine($"  Status: ready");
            System.Console.WriteLine($"  Path: {System.IO.Path.GetFullPath(_storePath)}");
            System.Console.WriteLine($"  Last indexed: {(lastModified != DateTime.MinValue ? lastModified.ToString("yyyy-MM-ddTHH:mm:ssZ") : "unknown")}");
            System.Console.WriteLine($"  Statistics:");
            System.Console.WriteLine($"    Method calls: {methodCallCount}");
            System.Console.WriteLine($"    Method definitions: {methodDefCount}");
            System.Console.WriteLine($"    Class definitions: {classDefCount}");
            System.Console.WriteLine($"    Total documents: {allIds.Length}");
            System.Console.WriteLine();
            System.Console.WriteLine("NOTE: Full project management (multiple projects with IDs) requires additional infrastructure.");
            System.Console.WriteLine("      Currently, the system uses a single vector store per instance.");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error listing projects: {ex.Message}");
        }
    }

    private static async Task ValidateApiCommandsAsync(string projectPath)
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
                System.Console.WriteLine($"Validating API commands for project: {projectPath}");
                System.Console.WriteLine("Re-analyzing source code to get ground truth...");
            }

            // Step 1: Get ground truth
            var tempAnalyzer = new RoslynAnalyzer();
            var groundTruth = await tempAnalyzer.AnalyzeProjectAsync(projectPath);

            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Ground truth: {groundTruth.MethodCalls.Count} calls, {groundTruth.MethodDefinitions.Count} methods, {groundTruth.ClassDefinitions.Count} classes");
                System.Console.WriteLine();
            }

            System.Console.WriteLine("=== API COMMAND ACCURACY REPORT ===");
            System.Console.WriteLine();

            // Test list-classes
            await TestListClassesAsync(groundTruth.ClassDefinitions);

            // Test list-methods
            await TestListMethodsAsync(groundTruth.MethodDefinitions);

            // Test get-method
            await TestGetMethodAsync(groundTruth.MethodDefinitions);

            // Test get-class
            await TestGetClassAsync(groundTruth.ClassDefinitions);

            // Test get-callers
            await TestGetCallersAsync(groundTruth.MethodCalls, groundTruth.MethodDefinitions);

            // Test get-callees
            await TestGetCalleesAsync(groundTruth.MethodCalls, groundTruth.MethodDefinitions);

            // Test get-class-methods
            await TestGetClassMethodsAsync(groundTruth.ClassDefinitions, groundTruth.MethodDefinitions);

            // Test get-class-references
            await TestGetClassReferencesAsync(groundTruth.ClassDefinitions, groundTruth.MethodCalls);

            // Test list-entry-points
            await TestListEntryPointsAsync(groundTruth.MethodDefinitions);

            System.Console.WriteLine("=== VALIDATION COMPLETE ===");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error validating API commands: {ex.Message}");
            if (_verbosity >= VerbosityLevel.Verbose)
            {
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private static async Task TestListClassesAsync(List<ClassDefinitionInfo> groundTruth)
    {
        if (_vectorStore == null) return;

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var returnedClasses = new HashSet<string>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" && metadata.ContainsKey("class"))
                {
                    returnedClasses.Add(metadata["class"]?.ToString() ?? string.Empty);
                }
            }

            var groundTruthSet = new HashSet<string>(groundTruth.Select(c => c.FullyQualifiedName));
            var correct = groundTruthSet.Intersect(returnedClasses).Count();
            var missing = groundTruthSet.Except(returnedClasses).Count();
            var extra = returnedClasses.Except(groundTruthSet).Count();
            var precision = returnedClasses.Count > 0 ? (double)correct / returnedClasses.Count : 0.0;
            var recall = groundTruthSet.Count > 0 ? (double)correct / groundTruthSet.Count : 0.0;
            var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0.0;

            System.Console.WriteLine("list-classes:");
            System.Console.WriteLine($"  Ground Truth: {groundTruthSet.Count}");
            System.Console.WriteLine($"  Returned: {returnedClasses.Count}");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Missing: {missing}");
            System.Console.WriteLine($"  Extra: {extra}");
            System.Console.WriteLine($"  Precision: {precision:P2}");
            System.Console.WriteLine($"  Recall: {recall:P2}");
            System.Console.WriteLine($"  F1 Score: {f1:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing list-classes: {ex.Message}");
        }
    }

    private static async Task TestListMethodsAsync(List<MethodDefinitionInfo> groundTruth)
    {
        if (_vectorStore == null) return;

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var returnedMethods = new HashSet<string>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && metadata.ContainsKey("method"))
                {
                    returnedMethods.Add(metadata["method"]?.ToString() ?? string.Empty);
                }
            }

            var groundTruthSet = new HashSet<string>(groundTruth.Select(m => m.FullyQualifiedName));
            var correct = groundTruthSet.Intersect(returnedMethods).Count();
            var missing = groundTruthSet.Except(returnedMethods).Count();
            var extra = returnedMethods.Except(groundTruthSet).Count();
            var precision = returnedMethods.Count > 0 ? (double)correct / returnedMethods.Count : 0.0;
            var recall = groundTruthSet.Count > 0 ? (double)correct / groundTruthSet.Count : 0.0;
            var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0.0;

            System.Console.WriteLine("list-methods:");
            System.Console.WriteLine($"  Ground Truth: {groundTruthSet.Count}");
            System.Console.WriteLine($"  Returned: {returnedMethods.Count}");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Missing: {missing}");
            System.Console.WriteLine($"  Extra: {extra}");
            System.Console.WriteLine($"  Precision: {precision:P2}");
            System.Console.WriteLine($"  Recall: {recall:P2}");
            System.Console.WriteLine($"  F1 Score: {f1:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing list-methods: {ex.Message}");
        }
    }

    private static async Task TestGetMethodAsync(List<MethodDefinitionInfo> groundTruth)
    {
        if (_vectorStore == null) return;

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var methodMap = new Dictionary<string, MethodDefinitionInfo>();

            // Build map of stored methods
            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && metadata.ContainsKey("method"))
                {
                    var fqn = metadata["method"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(fqn) && !methodMap.ContainsKey(fqn))
                    {
                        methodMap[fqn] = new MethodDefinitionInfo
                        {
                            FullyQualifiedName = fqn,
                            MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                            ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                            Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty
                        };
                    }
                }
            }

            int found = 0;
            int notFound = 0;

            foreach (var method in groundTruth)
            {
                if (methodMap.ContainsKey(method.FullyQualifiedName))
                {
                    found++;
                }
                else
                {
                    notFound++;
                }
            }

            var total = groundTruth.Count;
            var accuracy = total > 0 ? (double)found / total : 0.0;

            System.Console.WriteLine("get-method:");
            System.Console.WriteLine($"  Tested: {total} methods");
            System.Console.WriteLine($"  Found: {found}");
            System.Console.WriteLine($"  Not Found: {notFound}");
            System.Console.WriteLine($"  Accuracy: {accuracy:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing get-method: {ex.Message}");
        }
    }

    private static async Task TestGetClassAsync(List<ClassDefinitionInfo> groundTruth)
    {
        if (_vectorStore == null) return;

        try
        {
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var classMap = new HashSet<string>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" && metadata.ContainsKey("class"))
                {
                    classMap.Add(metadata["class"]?.ToString() ?? string.Empty);
                }
            }

            int found = 0;
            int notFound = 0;

            foreach (var cls in groundTruth)
            {
                if (classMap.Contains(cls.FullyQualifiedName))
                {
                    found++;
                }
                else
                {
                    notFound++;
                }
            }

            var total = groundTruth.Count;
            var accuracy = total > 0 ? (double)found / total : 0.0;

            System.Console.WriteLine("get-class:");
            System.Console.WriteLine($"  Tested: {total} classes");
            System.Console.WriteLine($"  Found: {found}");
            System.Console.WriteLine($"  Not Found: {notFound}");
            System.Console.WriteLine($"  Accuracy: {accuracy:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing get-class: {ex.Message}");
        }
    }

    private static async Task TestGetCallersAsync(List<MethodCallInfo> methodCalls, List<MethodDefinitionInfo> methods)
    {
        if (_vectorStore == null) return;

        try
        {
            // Build ground truth: callee -> callers
            var groundTruthCallers = new Dictionary<string, HashSet<string>>();
            foreach (var call in methodCalls)
            {
                if (!groundTruthCallers.ContainsKey(call.Callee))
                    groundTruthCallers[call.Callee] = new HashSet<string>();
                groundTruthCallers[call.Callee].Add(call.Caller);
            }

            // Build stored callers map
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedCallers = new Dictionary<string, HashSet<string>>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                    metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var callee = metadata["callee"]?.ToString() ?? string.Empty;
                    var caller = metadata["caller"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(callee) && !string.IsNullOrEmpty(caller))
                    {
                        if (!storedCallers.ContainsKey(callee))
                            storedCallers[callee] = new HashSet<string>();
                        storedCallers[callee].Add(caller);
                    }
                }
            }

            // Test a sample of methods that have callers
            var methodsWithCallers = groundTruthCallers.Keys.Take(Math.Min(50, groundTruthCallers.Count)).ToList();
            int correct = 0;
            int incorrect = 0;
            int totalCallers = 0;

            foreach (var callee in methodsWithCallers)
            {
                var expected = groundTruthCallers[callee];
                var actual = storedCallers.ContainsKey(callee) ? storedCallers[callee] : new HashSet<string>();

                var correctCallers = expected.Intersect(actual).Count();
                var missingCallers = expected.Except(actual).Count();
                var extraCallers = actual.Except(expected).Count();

                if (missingCallers == 0 && extraCallers == 0)
                    correct++;
                else
                    incorrect++;

                totalCallers += expected.Count;
            }

            var accuracy = methodsWithCallers.Count > 0 ? (double)correct / methodsWithCallers.Count : 0.0;

            System.Console.WriteLine("get-callers:");
            System.Console.WriteLine($"  Tested: {methodsWithCallers.Count} methods (sample)");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Incorrect: {incorrect}");
            System.Console.WriteLine($"  Total callers tested: {totalCallers}");
            System.Console.WriteLine($"  Accuracy: {accuracy:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing get-callers: {ex.Message}");
        }
    }

    private static async Task TestGetCalleesAsync(List<MethodCallInfo> methodCalls, List<MethodDefinitionInfo> methods)
    {
        if (_vectorStore == null) return;

        try
        {
            // Build ground truth: caller -> callees
            var groundTruthCallees = new Dictionary<string, HashSet<string>>();
            foreach (var call in methodCalls)
            {
                if (!groundTruthCallees.ContainsKey(call.Caller))
                    groundTruthCallees[call.Caller] = new HashSet<string>();
                groundTruthCallees[call.Caller].Add(call.Callee);
            }

            // Build stored callees map
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedCallees = new Dictionary<string, HashSet<string>>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                    metadata.ContainsKey("caller") && metadata.ContainsKey("callee"))
                {
                    var caller = metadata["caller"]?.ToString() ?? string.Empty;
                    var callee = metadata["callee"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(callee))
                    {
                        if (!storedCallees.ContainsKey(caller))
                            storedCallees[caller] = new HashSet<string>();
                        storedCallees[caller].Add(callee);
                    }
                }
            }

            // Test a sample of methods that have callees
            var methodsWithCallees = groundTruthCallees.Keys.Take(Math.Min(50, groundTruthCallees.Count)).ToList();
            int correct = 0;
            int incorrect = 0;
            int totalCallees = 0;

            foreach (var caller in methodsWithCallees)
            {
                var expected = groundTruthCallees[caller];
                var actual = storedCallees.ContainsKey(caller) ? storedCallees[caller] : new HashSet<string>();

                var correctCallees = expected.Intersect(actual).Count();
                var missingCallees = expected.Except(actual).Count();
                var extraCallees = actual.Except(expected).Count();

                if (missingCallees == 0 && extraCallees == 0)
                    correct++;
                else
                    incorrect++;

                totalCallees += expected.Count;
            }

            var accuracy = methodsWithCallees.Count > 0 ? (double)correct / methodsWithCallees.Count : 0.0;

            System.Console.WriteLine("get-callees:");
            System.Console.WriteLine($"  Tested: {methodsWithCallees.Count} methods (sample)");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Incorrect: {incorrect}");
            System.Console.WriteLine($"  Total callees tested: {totalCallees}");
            System.Console.WriteLine($"  Accuracy: {accuracy:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing get-callees: {ex.Message}");
        }
    }

    private static async Task TestGetClassMethodsAsync(List<ClassDefinitionInfo> classes, List<MethodDefinitionInfo> methods)
    {
        if (_vectorStore == null) return;

        try
        {
            // Build ground truth: class -> methods
            var groundTruthMethods = new Dictionary<string, HashSet<string>>();
            foreach (var method in methods)
            {
                if (!groundTruthMethods.ContainsKey(method.ClassName))
                    groundTruthMethods[method.ClassName] = new HashSet<string>();
                groundTruthMethods[method.ClassName].Add(method.FullyQualifiedName);
            }

            // Build stored methods map
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedMethods = new Dictionary<string, HashSet<string>>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" &&
                    metadata.ContainsKey("class") && metadata.ContainsKey("method"))
                {
                    var className = metadata["class"]?.ToString() ?? string.Empty;
                    var methodFqn = metadata["method"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(methodFqn))
                    {
                        if (!storedMethods.ContainsKey(className))
                            storedMethods[className] = new HashSet<string>();
                        storedMethods[className].Add(methodFqn);
                    }
                }
            }

            // Test classes that have methods
            var classesWithMethods = classes.Where(c => groundTruthMethods.ContainsKey(c.ClassName)).Take(Math.Min(20, classes.Count)).ToList();
            int correct = 0;
            int incorrect = 0;

            foreach (var cls in classesWithMethods)
            {
                var expected = groundTruthMethods[cls.ClassName];
                var actual = storedMethods.ContainsKey(cls.ClassName) ? storedMethods[cls.ClassName] : new HashSet<string>();

                var missing = expected.Except(actual).Count();
                var extra = actual.Except(expected).Count();

                if (missing == 0 && extra == 0)
                    correct++;
                else
                    incorrect++;
            }

            var accuracy = classesWithMethods.Count > 0 ? (double)correct / classesWithMethods.Count : 0.0;

            System.Console.WriteLine("get-class-methods:");
            System.Console.WriteLine($"  Tested: {classesWithMethods.Count} classes (sample)");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Incorrect: {incorrect}");
            System.Console.WriteLine($"  Accuracy: {accuracy:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing get-class-methods: {ex.Message}");
        }
    }

    private static async Task TestGetClassReferencesAsync(List<ClassDefinitionInfo> classes, List<MethodCallInfo> methodCalls)
    {
        if (_vectorStore == null) return;

        try
        {
            // Build ground truth: class -> referencing classes (via method calls)
            var groundTruthRefs = new Dictionary<string, HashSet<string>>();
            foreach (var call in methodCalls)
            {
                if (!string.IsNullOrEmpty(call.CalleeClass) && !string.IsNullOrEmpty(call.CallerClass))
                {
                    if (!groundTruthRefs.ContainsKey(call.CalleeClass))
                        groundTruthRefs[call.CalleeClass] = new HashSet<string>();
                    groundTruthRefs[call.CalleeClass].Add(call.CallerClass);
                }
            }

            // Build stored references map
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedRefs = new Dictionary<string, HashSet<string>>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                    metadata.ContainsKey("callee_class") && metadata.ContainsKey("caller_class"))
                {
                    var calleeClass = metadata["callee_class"]?.ToString() ?? string.Empty;
                    var callerClass = metadata["caller_class"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(calleeClass) && !string.IsNullOrEmpty(callerClass))
                    {
                        if (!storedRefs.ContainsKey(calleeClass))
                            storedRefs[calleeClass] = new HashSet<string>();
                        storedRefs[calleeClass].Add(callerClass);
                    }
                }
            }

            // Test classes that have references
            var classesWithRefs = groundTruthRefs.Keys.Take(Math.Min(20, groundTruthRefs.Count)).ToList();
            int correct = 0;
            int incorrect = 0;

            foreach (var className in classesWithRefs)
            {
                var expected = groundTruthRefs[className];
                var actual = storedRefs.ContainsKey(className) ? storedRefs[className] : new HashSet<string>();

                var missing = expected.Except(actual).Count();
                var extra = actual.Except(expected).Count();

                if (missing == 0 && extra == 0)
                    correct++;
                else
                    incorrect++;
            }

            var accuracy = classesWithRefs.Count > 0 ? (double)correct / classesWithRefs.Count : 0.0;

            System.Console.WriteLine("get-class-references:");
            System.Console.WriteLine($"  Tested: {classesWithRefs.Count} classes (sample)");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Incorrect: {incorrect}");
            System.Console.WriteLine($"  Accuracy: {accuracy:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing get-class-references: {ex.Message}");
        }
    }

    private static async Task TestListEntryPointsAsync(List<MethodDefinitionInfo> methods)
    {
        if (_vectorStore == null) return;

        try
        {
            // Ground truth: Main methods or methods in Controllers
            var groundTruthEntryPoints = new HashSet<string>();
            foreach (var method in methods)
            {
                bool isEntryPoint = method.MethodName == "Main" ||
                                   method.ClassName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
                if (isEntryPoint)
                {
                    groundTruthEntryPoints.Add(method.FullyQualifiedName);
                }
            }

            // Get stored entry points
            var allIds = await _vectorStore.Store.GetAllIdsAsync();
            var storedEntryPoints = new HashSet<string>();

            foreach (var id in allIds)
            {
                var doc = await _vectorStore.Store.GetAsync(id);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" && metadata.ContainsKey("method"))
                {
                    var methodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty;
                    var className = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;
                    var methodFqn = metadata["method"]?.ToString() ?? string.Empty;

                    bool isEntryPoint = methodName == "Main" ||
                                       className.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
                    if (isEntryPoint && !string.IsNullOrEmpty(methodFqn))
                    {
                        storedEntryPoints.Add(methodFqn);
                    }
                }
            }

            var correct = groundTruthEntryPoints.Intersect(storedEntryPoints).Count();
            var missing = groundTruthEntryPoints.Except(storedEntryPoints).Count();
            var extra = storedEntryPoints.Except(groundTruthEntryPoints).Count();
            var precision = storedEntryPoints.Count > 0 ? (double)correct / storedEntryPoints.Count : 0.0;
            var recall = groundTruthEntryPoints.Count > 0 ? (double)correct / groundTruthEntryPoints.Count : 0.0;
            var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0.0;

            System.Console.WriteLine("list-entry-points:");
            System.Console.WriteLine($"  Ground Truth: {groundTruthEntryPoints.Count}");
            System.Console.WriteLine($"  Returned: {storedEntryPoints.Count}");
            System.Console.WriteLine($"  Correct: {correct}");
            System.Console.WriteLine($"  Missing: {missing}");
            System.Console.WriteLine($"  Extra: {extra}");
            System.Console.WriteLine($"  Precision: {precision:P2}");
            System.Console.WriteLine($"  Recall: {recall:P2}");
            System.Console.WriteLine($"  F1 Score: {f1:P2}");
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error testing list-entry-points: {ex.Message}");
        }
    }

    private class AccuracyMetrics
    {
        public int Correct { get; set; }
        public int Missing { get; set; }
        public int Extra { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public List<string> MissingItems { get; set; } = new();
        public List<string> ExtraItems { get; set; } = new();
    }
}
