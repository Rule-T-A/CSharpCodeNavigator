namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents a caller method in the call graph.
/// </summary>
public class CallerInfo
{
    /// <summary>
    /// Fully qualified name of the caller method.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the caller method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the caller method.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the caller method.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Depth level in the call graph (1 = direct caller, 2 = caller of caller, etc.).
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Path to the source file containing the call.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the call occurs (1-based).
    /// </summary>
    public int LineNumber { get; set; }
}

