namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for listing methods.
/// </summary>
public class MethodListResponse
{
    /// <summary>
    /// List of methods matching the query.
    /// </summary>
    public List<MethodInfo> Methods { get; set; } = new();

    /// <summary>
    /// Total number of methods matching the query (before pagination).
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

