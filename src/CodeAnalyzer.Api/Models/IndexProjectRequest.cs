namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Request model for indexing a project.
/// </summary>
public class IndexProjectRequest
{
    /// <summary>
    /// Path to the .csproj file or project directory.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name for the project. If not provided, will be derived from project path.
    /// </summary>
    public string? ProjectName { get; set; }
}

