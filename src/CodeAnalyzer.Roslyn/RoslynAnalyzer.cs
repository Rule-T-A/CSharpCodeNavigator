using CodeAnalyzer.Roslyn.Models;

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
}
