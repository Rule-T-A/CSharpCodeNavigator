namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents a method in the enumeration response.
/// </summary>
public class MethodInfo
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
    /// Name of the class containing the method.
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
}

