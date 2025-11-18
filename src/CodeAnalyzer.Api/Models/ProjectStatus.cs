namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents the current status of a project indexing operation.
/// </summary>
public class ProjectStatus
{
    /// <summary>
    /// Unique identifier for the project.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the indexing operation.
    /// </summary>
    public IndexingStatus Status { get; set; }

    /// <summary>
    /// Progress percentage (0-100) for indexing operations.
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Current message describing the status.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Any errors encountered during indexing.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when indexing started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when indexing completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Status values for project indexing operations.
/// </summary>
public enum IndexingStatus
{
    /// <summary>
    /// Project is queued for indexing.
    /// </summary>
    Queued,

    /// <summary>
    /// Project is currently being indexed.
    /// </summary>
    Indexing,

    /// <summary>
    /// Project indexing completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Project indexing failed with errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Project has not been indexed yet.
    /// </summary>
    NotIndexed
}

