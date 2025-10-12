using CodeAnalyzer.Roslyn.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Roslyn;

/// <summary>
/// Analyzes C# projects using Roslyn to extract method call relationships.
/// This is the main entry point for Phase 1 of the C# Code Navigator.
/// </summary>
public class RoslynAnalyzer
{
    private readonly IVectorStoreWriter? _vectorStore;
    private readonly AnalyzerOptions _options = new();
    /// <summary>
    /// Creates a new instance of RoslynAnalyzer
    /// </summary>
    public RoslynAnalyzer()
    {
    }

    /// <summary>
    /// Optional constructor to enable persistence to a vector store
    /// </summary>
    public RoslynAnalyzer(IVectorStoreWriter vectorStore)
    {
        _vectorStore = vectorStore;
    }

    public RoslynAnalyzer WithOptions(AnalyzerOptions options)
    {
        if (options != null)
        {
            _options.IncludeWarningsInErrors = options.IncludeWarningsInErrors;
            _options.RecordExternalCalls = options.RecordExternalCalls;
            _options.AttributeInitializerCalls = options.AttributeInitializerCalls;
        }
        return this;
    }

    /// <summary>
    /// Analyzes a C# project and extracts all method call relationships.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file or project directory</param>
    /// <returns>AnalysisResult containing all discovered method calls and metadata</returns>
    public async Task<AnalysisResult> AnalyzeProjectAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required", nameof(projectPath));

        var result = new AnalysisResult
        {
            FilesProcessed = 0,
            MethodsAnalyzed = 0,
            MethodCalls = new List<MethodCallInfo>(),
            Errors = new List<string>()
        };

        try
        {
            var compilation = await CreateCompilationAsync(projectPath).ConfigureAwait(false);

            // Collect diagnostics (non-fatal)
            var diagnostics = compilation.GetDiagnostics();
            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Error || (_options.IncludeWarningsInErrors && d.Severity == DiagnosticSeverity.Warning))
                    result.Errors.Add(d.ToString());
            }

            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var methods = ExtractMethodDeclarations(tree, model);
                result.MethodsAnalyzed += methods.Count;

                var calls = ExtractMethodCalls(tree, model);
                result.MethodCalls.AddRange(calls);
                result.FilesProcessed += 1;

                if (_vectorStore != null && calls.Count > 0)
                {
                    foreach (var call in calls)
                    {
                        try
                        {
                            await StoreMethodCallAsync(call).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Analyzes a single C# file and extracts method call relationships.
    /// </summary>
    /// <param name="filePath">Path to the .cs file</param>
    /// <returns>AnalysisResult containing method calls found in the file</returns>
    public async Task<AnalysisResult> AnalyzeFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));

        var compilation = await CreateCompilationFromFilesAsync(filePath).ConfigureAwait(false);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);

        var result = new AnalysisResult
        {
            FilesProcessed = 1,
            MethodsAnalyzed = 0,
            MethodCalls = new List<MethodCallInfo>(),
            Errors = new List<string>()
        };

        try
        {
            var methods = ExtractMethodDeclarations(tree, model);
            result.MethodsAnalyzed = methods.Count;

            var calls = ExtractMethodCalls(tree, model);
            result.MethodCalls.AddRange(calls);

            // Collect file-specific diagnostics if requested in file mode
            var diags = model.Compilation.GetDiagnostics();
            foreach (var d in diags)
            {
                if (d.Severity == DiagnosticSeverity.Error || (_options.IncludeWarningsInErrors && d.Severity == DiagnosticSeverity.Warning))
                    result.Errors.Add(d.ToString());
            }

            if (_vectorStore != null && calls.Count > 0)
            {
                foreach (var call in calls)
                {
                    try
                    {
                        await StoreMethodCallAsync(call).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Gets the version of the analyzer for debugging purposes
    /// </summary>
    public string Version => "1.0.0-phase1";

    /// <summary>
    /// Create a Roslyn Compilation for a C# project using MSBuildWorkspace.
    /// </summary>
    /// <param name="projectPath">Path to a .csproj file</param>
    /// <returns>Compilation with all syntax trees and references loaded</returns>
    public async Task<Compilation> CreateCompilationAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required", nameof(projectPath));

        // Ensure MSBuild is registered once per process
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath).ConfigureAwait(false);
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
        if (compilation == null)
            throw new InvalidOperationException("Failed to create compilation for project");

        return compilation;
    }

    /// <summary>
    /// Create a Roslyn Compilation from one or more C# source files without a project file.
    /// </summary>
    /// <param name="filePaths">Paths to .cs files</param>
    /// <returns>Compilation suitable for basic semantic analysis</returns>
    public async Task<Compilation> CreateCompilationFromFilesAsync(params string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            throw new ArgumentException("At least one C# file path is required", nameof(filePaths));

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var path in filePaths)
        {
            var source = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var tree = CSharpSyntaxTree.ParseText(source, path: path);
            syntaxTrees.Add(tree);
        }

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalysisCompilation",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        return compilation;
    }

    /// <summary>
    /// Extract fully qualified method names declared in the given syntax tree.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of fully qualified method names</returns>
    public List<string> ExtractMethodDeclarations(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<string>();

        foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(methodDecl);
            if (symbol == null)
                continue;

            var fqn = GetFullyQualifiedName(symbol);
            if (!string.IsNullOrWhiteSpace(fqn))
                results.Add(fqn);
        }

        return results;
    }

    /// <summary>
    /// Generate fully qualified name for a symbol as Namespace.Class.Method
    /// </summary>
    /// <param name="symbol">Symbol to format</param>
    /// <returns>Fully qualified name</returns>
    public string GetFullyQualifiedName(ISymbol symbol)
    {
        if (symbol == null) return string.Empty;

        var parts = new List<string>();
        parts.Add(symbol.Name);

        if (symbol.ContainingType != null)
            parts.Insert(0, symbol.ContainingType.Name);

        var ns = symbol.ContainingNamespace;
        if (ns != null && !ns.IsGlobalNamespace)
            parts.Insert(0, ns.ToDisplayString());

        return string.Join('.', parts);
    }

    /// <summary>
    /// Extract method call relationships from a syntax tree using the provided semantic model.
    /// </summary>
    public List<MethodCallInfo> ExtractMethodCalls(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<MethodCallInfo>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var callerSymbol = GetContainingMethodSymbol(model, invocation);
            if (callerSymbol == null)
                continue; // skip invocations outside methods

            var symbolInfo = model.GetSymbolInfo(invocation);
            IMethodSymbol? calleeSymbol = symbolInfo.Symbol as IMethodSymbol;
            if (calleeSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                calleeSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            }
            if (calleeSymbol == null)
                continue; // unresolved; skip

            // Normalize extension method to the defining static method
            if (calleeSymbol.ReducedFrom is IMethodSymbol reduced)
            {
                calleeSymbol = reduced;
            }

            // Normalize inheritance/interface dispatch targets per 1.5
            calleeSymbol = NormalizeCalleeSymbol(calleeSymbol);

            var location = invocation.GetLocation().GetLineSpan();

            var call = new MethodCallInfo
            {
                Caller = GetFullyQualifiedName(callerSymbol),
                Callee = GetFullyQualifiedName(calleeSymbol),
                CallerClass = callerSymbol.ContainingType?.Name ?? string.Empty,
                CalleeClass = calleeSymbol.ContainingType?.Name ?? string.Empty,
                CallerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                CalleeNamespace = calleeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                FilePath = location.Path,
                LineNumber = location.StartLinePosition.Line + 1
            };

            results.Add(call);
        }

        // Process attribute constructor calls and initializer calls if enabled
        if (_options.AttributeInitializerCalls)
        {
            // Process attribute constructor calls
            var attributeCalls = ExtractAttributeConstructorCalls(tree, model);
            results.AddRange(attributeCalls);
            
            // Process field/property initializer calls  
            var initializerCalls = ExtractInitializerCalls(tree, model);
            results.AddRange(initializerCalls);
        }

        return results;
    }

    /// <summary>
    /// Find the IMethodSymbol that contains the given syntax node.
    /// </summary>
    public IMethodSymbol? GetContainingMethodSymbol(SemanticModel model, SyntaxNode node)
    {
        var methodDecl = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl == null) return null;
        return model.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
    }

    private IMethodSymbol NormalizeCalleeSymbol(IMethodSymbol callee)
    {
        // Explicit interface implementation: map to the interface method
        if (callee.ExplicitInterfaceImplementations != null && callee.ExplicitInterfaceImplementations.Length > 0)
        {
            return callee.ExplicitInterfaceImplementations[0];
        }

        // Interface calls: already an interface symbol; keep as-is
        if (callee.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return callee;
        }

        // Overrides/virtual: normalize to the top-most overridden base method
        var current = callee;
        while (current.OverriddenMethod != null)
        {
            current = current.OverriddenMethod;
        }
        return current;
    }

    /// <summary>
    /// Extract method calls from attribute constructor invocations.
    /// </summary>
    private List<MethodCallInfo> ExtractAttributeConstructorCalls(SyntaxTree tree, SemanticModel model)
    {
        var results = new List<MethodCallInfo>();
        var root = tree.GetRoot();

        foreach (var attributeList in root.DescendantNodes().OfType<AttributeListSyntax>())
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = model.GetSymbolInfo(attribute);
                var constructorSymbol = symbolInfo.Symbol as IMethodSymbol;
                if (constructorSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
                {
                    constructorSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
                }
                if (constructorSymbol == null)
                    continue; // unresolved; skip

                // Determine the caller (containing member or type)
                var callerSymbol = GetContainingMemberOrTypeSymbol(model, attribute);
                if (callerSymbol == null)
                    continue;

                var location = attribute.GetLocation().GetLineSpan();

                var call = new MethodCallInfo
                {
                    Caller = GetFullyQualifiedName(callerSymbol),
                    Callee = GetFullyQualifiedName(constructorSymbol),
                    CallerClass = callerSymbol.ContainingType?.Name ?? string.Empty,
                    CalleeClass = constructorSymbol.ContainingType?.Name ?? string.Empty,
                    CallerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    CalleeNamespace = constructorSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    FilePath = location.Path,
                    LineNumber = location.StartLinePosition.Line + 1
                };

                results.Add(call);
            }
        }

        return results;
    }

    /// <summary>
    /// Extract method calls from field and property initializers.
    /// </summary>
    private List<MethodCallInfo> ExtractInitializerCalls(SyntaxTree tree, SemanticModel model)
    {
        var results = new List<MethodCallInfo>();
        var root = tree.GetRoot();

        // Process field initializers
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (field.Declaration?.Variables != null)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Initializer?.Value != null)
                    {
                        var initializerCalls = ExtractMethodCallsFromExpression(variable.Initializer.Value, model, field.Declaration.Type);
                        results.AddRange(initializerCalls);
                    }
                }
            }
        }

        // Process property initializers
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (property.Initializer?.Value != null)
            {
                var initializerCalls = ExtractMethodCallsFromExpression(property.Initializer.Value, model, property.Type);
                results.AddRange(initializerCalls);
            }
        }

        return results;
    }

    /// <summary>
    /// Extract method calls from an expression (used for initializers).
    /// </summary>
    private List<MethodCallInfo> ExtractMethodCallsFromExpression(ExpressionSyntax expression, SemanticModel model, TypeSyntax containingType)
    {
        var results = new List<MethodCallInfo>();

        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            var symbolInfo = model.GetSymbolInfo(invocation);
            var calleeSymbol = symbolInfo.Symbol as IMethodSymbol;
            if (calleeSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                calleeSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            }
            if (calleeSymbol == null)
                continue; // unresolved; skip

            // Normalize extension method to the defining static method
            if (calleeSymbol.ReducedFrom is IMethodSymbol reduced)
            {
                calleeSymbol = reduced;
            }

            // Normalize inheritance/interface dispatch targets per 1.5
            calleeSymbol = NormalizeCalleeSymbol(calleeSymbol);

            // For initializers, the caller is the containing type
            var callerSymbol = GetContainingTypeSymbol(model, containingType);
            if (callerSymbol == null)
                continue;

            var location = invocation.GetLocation().GetLineSpan();

            var call = new MethodCallInfo
            {
                Caller = GetFullyQualifiedName(callerSymbol),
                Callee = GetFullyQualifiedName(calleeSymbol),
                CallerClass = callerSymbol.Name,
                CalleeClass = calleeSymbol.ContainingType?.Name ?? string.Empty,
                CallerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                CalleeNamespace = calleeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                FilePath = location.Path,
                LineNumber = location.StartLinePosition.Line + 1
            };

            results.Add(call);
        }

        return results;
    }

    /// <summary>
    /// Find the containing member (method, property, field) or type symbol for an attribute.
    /// </summary>
    private ISymbol? GetContainingMemberOrTypeSymbol(SemanticModel model, AttributeSyntax attribute)
    {
        // Walk up the syntax tree to find the containing declaration
        var containingNode = attribute.Ancestors().FirstOrDefault(node => 
            node is MethodDeclarationSyntax || 
            node is PropertyDeclarationSyntax || 
            node is FieldDeclarationSyntax || 
            node is ClassDeclarationSyntax || 
            node is StructDeclarationSyntax ||
            node is InterfaceDeclarationSyntax);

        if (containingNode == null)
            return null;

        return model.GetDeclaredSymbol(containingNode);
    }

    /// <summary>
    /// Find the containing type symbol for a type syntax.
    /// </summary>
    private INamedTypeSymbol? GetContainingTypeSymbol(SemanticModel model, TypeSyntax typeSyntax)
    {
        // Walk up the syntax tree to find the containing type declaration
        var containingNode = typeSyntax.Ancestors().FirstOrDefault(node => 
            node is ClassDeclarationSyntax || 
            node is StructDeclarationSyntax ||
            node is InterfaceDeclarationSyntax);

        if (containingNode == null)
            return null;

        return model.GetDeclaredSymbol(containingNode) as INamedTypeSymbol;
    }

    private async Task StoreMethodCallAsync(MethodCallInfo call)
    {
        if (_vectorStore == null)
            return;

        // Validate and normalize metadata before storing
        var validationResult = ValidateAndNormalizeMetadata(call);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Invalid method call metadata: {string.Join(", ", validationResult.Errors)}");
        }

        var content = $"Method {validationResult.NormalizedCall.Caller} in class {validationResult.NormalizedCall.CallerClass} calls method {validationResult.NormalizedCall.Callee} in class {validationResult.NormalizedCall.CalleeClass}. This call happens in file {validationResult.NormalizedCall.FilePath} at line {validationResult.NormalizedCall.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "method_call",
            ["caller"] = validationResult.NormalizedCall.Caller,
            ["callee"] = validationResult.NormalizedCall.Callee,
            ["caller_class"] = validationResult.NormalizedCall.CallerClass,
            ["callee_class"] = validationResult.NormalizedCall.CalleeClass,
            ["caller_namespace"] = validationResult.NormalizedCall.CallerNamespace,
            ["callee_namespace"] = validationResult.NormalizedCall.CalleeNamespace,
            ["file_path"] = validationResult.NormalizedCall.FilePath,
            ["line_number"] = validationResult.NormalizedCall.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and normalizes method call metadata according to the required schema.
    /// </summary>
    /// <param name="call">The method call to validate</param>
    /// <returns>Validation result with normalized data or errors</returns>
    public MetadataValidationResult ValidateAndNormalizeMetadata(MethodCallInfo call)
    {
        var errors = new List<string>();
        var normalizedCall = new MethodCallInfo();

        // Required field validation and normalization
        normalizedCall.Caller = NormalizeFullyQualifiedName(call.Caller, "caller", errors);
        normalizedCall.Callee = NormalizeFullyQualifiedName(call.Callee, "callee", errors);
        normalizedCall.CallerClass = NormalizeClassName(call.CallerClass, "caller_class", errors);
        normalizedCall.CalleeClass = NormalizeClassName(call.CalleeClass, "callee_class", errors);
        normalizedCall.CallerNamespace = NormalizeNamespace(call.CallerNamespace, "caller_namespace", errors);
        normalizedCall.CalleeNamespace = NormalizeNamespace(call.CalleeNamespace, "callee_namespace", errors);
        normalizedCall.FilePath = NormalizeFilePath(call.FilePath, "file_path", errors);
        normalizedCall.LineNumber = NormalizeLineNumber(call.LineNumber, "line_number", errors);

        return new MetadataValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            NormalizedCall = normalizedCall
        };
    }

    private string NormalizeFullyQualifiedName(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Required field '{fieldName}' is missing or empty");
            return string.Empty;
        }

        var normalized = value.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add($"Required field '{fieldName}' is empty after trimming whitespace");
            return string.Empty;
        }

        return normalized;
    }

    private string NormalizeClassName(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Required field '{fieldName}' is missing or empty");
            return string.Empty;
        }

        var normalized = value.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add($"Required field '{fieldName}' is empty after trimming whitespace");
            return string.Empty;
        }

        return normalized;
    }

    private string NormalizeNamespace(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Namespace can be empty for global namespace
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized;
    }

    private string NormalizeFilePath(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Required field '{fieldName}' is missing or empty");
            return string.Empty;
        }

        var normalized = value.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add($"Required field '{fieldName}' is empty after trimming whitespace");
            return string.Empty;
        }

        // Convert to relative path if absolute
        try
        {
            if (Path.IsPathRooted(normalized))
            {
                // For now, keep absolute paths as-is, but could normalize to relative
                // normalized = Path.GetRelativePath(Directory.GetCurrentDirectory(), normalized);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Invalid file path '{normalized}': {ex.Message}");
            return string.Empty;
        }

        return normalized;
    }

    private int NormalizeLineNumber(int value, string fieldName, List<string> errors)
    {
        if (value < 1)
        {
            errors.Add($"Required field '{fieldName}' must be >= 1, got {value}");
            return 1; // Default to line 1
        }

        return value;
    }
}
