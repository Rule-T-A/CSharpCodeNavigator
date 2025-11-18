namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents a class that references another class.
/// </summary>
public class ClassReferenceInfo
{
    /// <summary>
    /// Fully qualified name of the referencing class.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the referencing class.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the referencing class.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Type of relationship (e.g., "calls", "inherits", "implements").
    /// </summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>
    /// Path to the source file containing the reference.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the reference occurs (1-based).
    /// </summary>
    public int LineNumber { get; set; }
}

