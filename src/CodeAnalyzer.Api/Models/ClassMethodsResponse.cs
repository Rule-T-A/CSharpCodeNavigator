namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Response containing all methods in a class.
/// </summary>
public class ClassMethodsResponse
{
    /// <summary>
    /// Fully qualified name of the class.
    /// </summary>
    public string ClassFullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// List of methods in the class.
    /// </summary>
    public List<MethodInfo> Methods { get; set; } = new();

    /// <summary>
    /// Total number of methods in the class.
    /// </summary>
    public int TotalCount { get; set; }
}

