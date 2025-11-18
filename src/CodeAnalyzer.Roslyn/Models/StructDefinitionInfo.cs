namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents a struct definition discovered during code analysis.
/// Contains all metadata needed to store the struct definition in the vector database.
/// </summary>
public class StructDefinitionInfo
{
    /// <summary>
    /// Name of the struct
    /// </summary>
    public string StructName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the struct
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the struct
    /// Format: Namespace.StructName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier of the struct (public, private, protected, internal)
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the struct is readonly
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the struct is a ref struct
    /// </summary>
    public bool IsRef { get; set; }

    /// <summary>
    /// List of implemented interfaces
    /// </summary>
    public List<string> Interfaces { get; set; } = new();

    /// <summary>
    /// Number of methods in the struct
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of properties in the struct
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Number of fields in the struct
    /// </summary>
    public int FieldCount { get; set; }

    /// <summary>
    /// Path to the source file containing the struct definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the struct definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of StructDefinitionInfo
    /// </summary>
    public StructDefinitionInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of StructDefinitionInfo with the specified values
    /// </summary>
    public StructDefinitionInfo(
        string structName,
        string namespaceName,
        string fullyQualifiedName,
        string accessModifier,
        bool isReadOnly,
        bool isRef,
        List<string> interfaces,
        int methodCount,
        int propertyCount,
        int fieldCount,
        string filePath,
        int lineNumber)
    {
        StructName = structName;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        AccessModifier = accessModifier;
        IsReadOnly = isReadOnly;
        IsRef = isRef;
        Interfaces = interfaces ?? new List<string>();
        MethodCount = methodCount;
        PropertyCount = propertyCount;
        FieldCount = fieldCount;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the struct definition
    /// </summary>
    public override string ToString()
    {
        var modifiers = "";
        if (IsReadOnly) modifiers += "readonly ";
        if (IsRef) modifiers += "ref ";

        var inheritance = Interfaces.Count > 0 ? $" : {string.Join(", ", Interfaces)}" : "";

        return $"{AccessModifier} {modifiers.Trim()}struct {StructName}{inheritance} (line {LineNumber} in {FilePath})";
    }
}

