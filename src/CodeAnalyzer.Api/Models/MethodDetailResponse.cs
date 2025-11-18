namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Detailed information about a method.
/// </summary>
public class MethodDetailResponse
{
    /// <summary>
    /// Fully qualified name of the method.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the class containing the method.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the method.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Return type of the method.
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// List of parameter types.
    /// </summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>
    /// Access modifier of the method.
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the method is static.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether the method is virtual.
    /// </summary>
    public bool IsVirtual { get; set; }

    /// <summary>
    /// Whether the method is abstract.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Whether the method is an override.
    /// </summary>
    public bool IsOverride { get; set; }

    /// <summary>
    /// Path to the source file containing the method definition.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the method definition starts (1-based).
    /// </summary>
    public int LineNumber { get; set; }
}

