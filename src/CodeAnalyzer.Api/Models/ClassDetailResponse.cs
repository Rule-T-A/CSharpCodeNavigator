namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Detailed information about a class.
/// </summary>
public class ClassDetailResponse
{
    /// <summary>
    /// Fully qualified name of the class.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the class.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier of the class.
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the class is static.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether the class is abstract.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Whether the class is sealed.
    /// </summary>
    public bool IsSealed { get; set; }

    /// <summary>
    /// Base class name (if any).
    /// </summary>
    public string? BaseClass { get; set; }

    /// <summary>
    /// List of implemented interfaces.
    /// </summary>
    public List<string> Interfaces { get; set; } = new();

    /// <summary>
    /// Number of methods in the class.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of properties in the class.
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Number of fields in the class.
    /// </summary>
    public int FieldCount { get; set; }

    /// <summary>
    /// Path to the source file containing the class definition.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the class definition starts (1-based).
    /// </summary>
    public int LineNumber { get; set; }
}

