using CodeAnalyzer.Roslyn.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

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
        // TODO: Implement single file analysis
        // This will be implemented in Step 1.2
        throw new NotImplementedException("File analysis not yet implemented");
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
}
