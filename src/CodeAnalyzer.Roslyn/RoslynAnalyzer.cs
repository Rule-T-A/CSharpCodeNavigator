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
    /// <summary>
    /// Creates a new instance of RoslynAnalyzer
    /// </summary>
    public RoslynAnalyzer()
    {
    }

    /// <summary>
    /// Analyzes a C# project and extracts all method call relationships.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file or project directory</param>
    /// <returns>AnalysisResult containing all discovered method calls and metadata</returns>
    public async Task<AnalysisResult> AnalyzeProjectAsync(string projectPath)
    {
        // TODO: Implement project analysis
        // This will be implemented in Step 1.2
        throw new NotImplementedException("Project analysis not yet implemented");
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
            if (symbolInfo.Symbol is not IMethodSymbol calleeSymbol)
                continue; // unresolved; handled in Step 1.5

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
}
