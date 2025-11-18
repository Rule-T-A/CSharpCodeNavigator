namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for indexing a project.
/// </summary>
public class IndexProjectResponse
{
    /// <summary>
    /// Unique identifier for the indexed project.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the indexing operation.
    /// </summary>
    public IndexingStatus Status { get; set; }

    /// <summary>
    /// Message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

