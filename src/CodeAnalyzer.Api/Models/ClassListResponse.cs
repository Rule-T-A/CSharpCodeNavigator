namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for listing classes.
/// </summary>
public class ClassListResponse
{
    /// <summary>
    /// List of classes matching the query.
    /// </summary>
    public List<ClassInfo> Classes { get; set; } = new();

    /// <summary>
    /// Total number of classes matching the query (before pagination).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of items returned in this page.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Offset used for pagination.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Limit used for pagination.
    /// </summary>
    public int Limit { get; set; }
}

