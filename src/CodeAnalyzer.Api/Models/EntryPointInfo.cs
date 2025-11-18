namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents an entry point (Main method or controller action) in the enumeration response.
/// </summary>
public class EntryPointInfo
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
    /// Type of entry point (e.g., "Main", "Controller", "ApiController").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method for controller actions (e.g., "GET", "POST").
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Route template for controller actions.
    /// </summary>
    public string? Route { get; set; }
}

