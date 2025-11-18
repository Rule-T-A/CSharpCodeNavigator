namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for listing entry points.
/// </summary>
public class EntryPointListResponse
{
    /// <summary>
    /// List of entry points matching the query.
    /// </summary>
    public List<EntryPointInfo> EntryPoints { get; set; } = new();

    /// <summary>
    /// Total number of entry points found.
    /// </summary>
    public int TotalCount { get; set; }
}

