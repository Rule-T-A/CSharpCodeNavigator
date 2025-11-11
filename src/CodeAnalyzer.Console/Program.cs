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
        System.Console.WriteLine("  cleanup <project>      - Remove stale data not in current codebase");
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
                System.Console.WriteLine($"Analysis complete! Files: {result.FilesProcessed}, Methods: {result.MethodsAnalyzed}, Calls: {result.MethodCalls.Count}, Definitions: {result.MethodDefinitions.Count}, Classes: {result.ClassDefinitions.Count}");
            }
            
            if (_verbosity >= VerbosityLevel.Normal)
            {
                System.Console.WriteLine($"Analysis complete!");
                System.Console.WriteLine($"  Files processed: {result.FilesProcessed}");
                System.Console.WriteLine($"  Methods analyzed: {result.MethodsAnalyzed}");
                System.Console.WriteLine($"  Method calls found: {result.MethodCalls.Count}");
                System.Console.WriteLine($"  Method definitions found: {result.MethodDefinitions.Count}");
                System.Console.WriteLine($"  Class definitions found: {result.ClassDefinitions.Count}");
                
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
