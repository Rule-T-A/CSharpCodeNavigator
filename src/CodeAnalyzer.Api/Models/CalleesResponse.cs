namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for getting callees of a method.
/// </summary>
public class CalleesResponse
{
    /// <summary>
    /// Fully qualified name of the method whose callees were queried.
    /// </summary>
    public string MethodFullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// List of callee methods.
    /// </summary>
    public List<CalleeInfo> Callees { get; set; } = new();

    /// <summary>
    /// Total number of callees found.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Maximum depth that was traversed.
    /// </summary>
    public int MaxDepth { get; set; }
}

