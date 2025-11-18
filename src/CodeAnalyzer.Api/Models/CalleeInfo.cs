namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents a callee method in the call graph.
/// </summary>
public class CalleeInfo
{
    /// <summary>
    /// Fully qualified name of the callee method.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the callee method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the callee method.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the callee method.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Depth level in the call graph (1 = direct callee, 2 = callee of callee, etc.).
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

