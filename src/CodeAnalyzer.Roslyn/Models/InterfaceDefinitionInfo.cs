namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents an interface definition discovered during code analysis.
/// Contains all metadata needed to store the interface definition in the vector database.
/// </summary>
public class InterfaceDefinitionInfo
{
    /// <summary>
    /// Name of the interface
    /// </summary>
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the interface
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the interface
    /// Format: Namespace.InterfaceName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier of the interface (public, private, protected, internal)
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// List of inherited interfaces
    /// </summary>
    public List<string> BaseInterfaces { get; set; } = new();

    /// <summary>
    /// Number of methods in the interface
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of properties in the interface
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Path to the source file containing the interface definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the interface definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of InterfaceDefinitionInfo
    /// </summary>
    public InterfaceDefinitionInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of InterfaceDefinitionInfo with the specified values
    /// </summary>
    public InterfaceDefinitionInfo(
        string interfaceName,
        string namespaceName,
        string fullyQualifiedName,
        string accessModifier,
        List<string> baseInterfaces,
        int methodCount,
        int propertyCount,
        string filePath,
        int lineNumber)
    {
        InterfaceName = interfaceName;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        AccessModifier = accessModifier;
        BaseInterfaces = baseInterfaces ?? new List<string>();
        MethodCount = methodCount;
        PropertyCount = propertyCount;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the interface definition
    /// </summary>
    public override string ToString()
    {
        var inheritance = BaseInterfaces.Count > 0 ? $" : {string.Join(", ", BaseInterfaces)}" : "";
        return $"{AccessModifier} interface {InterfaceName}{inheritance} (line {LineNumber} in {FilePath})";
    }
}

