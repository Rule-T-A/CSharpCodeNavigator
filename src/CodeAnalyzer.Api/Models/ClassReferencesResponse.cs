namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response model for getting class references.
/// </summary>
public class ClassReferencesResponse
{
    /// <summary>
    /// Fully qualified name of the class whose references were queried.
    /// </summary>
    public string ClassFullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// List of classes that reference the target class.
    /// </summary>
    public List<ClassReferenceInfo> References { get; set; } = new();

    /// <summary>
    /// Total number of references found.
    /// </summary>
    public int TotalCount { get; set; }
}

