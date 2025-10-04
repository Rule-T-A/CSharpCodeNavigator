namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents a method call relationship discovered during code analysis.
/// Contains all metadata needed to store the relationship in the vector database.
/// </summary>
public class MethodCallInfo
{
    /// <summary>
    /// Fully qualified name of the method making the call (caller)
    /// Format: Namespace.ClassName.MethodName
    /// </summary>
    public string Caller { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the method being called (callee)
    /// Format: Namespace.ClassName.MethodName
    /// </summary>
    public string Callee { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the caller method
    /// </summary>
    public string CallerClass { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the callee method
    /// </summary>
    public string CalleeClass { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the caller method
    /// </summary>
    public string CallerNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the callee method
    /// </summary>
    public string CalleeNamespace { get; set; } = string.Empty;

    /// <summary>
    /// Path to the source file containing the method call
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the method call occurs (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of MethodCallInfo
    /// </summary>
    public MethodCallInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of MethodCallInfo with the specified values
    /// </summary>
    public MethodCallInfo(
        string caller,
        string callee,
        string callerClass,
        string calleeClass,
        string callerNamespace,
        string calleeNamespace,
        string filePath,
        int lineNumber)
    {
        Caller = caller;
        Callee = callee;
        CallerClass = callerClass;
        CalleeClass = calleeClass;
        CallerNamespace = callerNamespace;
        CalleeNamespace = calleeNamespace;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the method call
    /// </summary>
    public override string ToString()
    {
        return $"{Caller} -> {Callee} (line {LineNumber} in {FilePath})";
    }
}
