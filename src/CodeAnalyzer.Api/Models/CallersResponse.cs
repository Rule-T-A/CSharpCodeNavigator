namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for getting callers of a method.
/// </summary>
public class CallersResponse
{
    /// <summary>
    /// Fully qualified name of the method whose callers were queried.
    /// </summary>
    public string MethodFullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// List of caller methods.
    /// </summary>
    public List<CallerInfo> Callers { get; set; } = new();

    /// <summary>
    /// Total number of callers found.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Maximum depth that was traversed.
    /// </summary>
    public int MaxDepth { get; set; }
}

