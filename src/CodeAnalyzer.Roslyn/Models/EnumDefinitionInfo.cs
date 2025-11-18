namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents an enum definition discovered during code analysis.
/// Contains all metadata needed to store the enum definition in the vector database.
/// </summary>
public class EnumDefinitionInfo
{
    /// <summary>
    /// Name of the enum
    /// </summary>
    public string EnumName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the enum
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the enum
    /// Format: Namespace.EnumName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier of the enum (public, private, protected, internal)
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Underlying type of the enum (int, byte, long, etc.)
    /// </summary>
    public string UnderlyingType { get; set; } = "int";

    /// <summary>
    /// List of enum values
    /// </summary>
    public List<EnumValueInfo> Values { get; set; } = new();

    /// <summary>
    /// Path to the source file containing the enum definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the enum definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of EnumDefinitionInfo
    /// </summary>
    public EnumDefinitionInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of EnumDefinitionInfo with the specified values
    /// </summary>
    public EnumDefinitionInfo(
        string enumName,
        string namespaceName,
        string fullyQualifiedName,
        string accessModifier,
        string underlyingType,
        List<EnumValueInfo> values,
        string filePath,
        int lineNumber)
    {
        EnumName = enumName;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        AccessModifier = accessModifier;
        UnderlyingType = underlyingType;
        Values = values ?? new List<EnumValueInfo>();
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the enum definition
    /// </summary>
    public override string ToString()
    {
        var valuesStr = string.Join(", ", Values.Select(v => v.ToString()));
        return $"{AccessModifier} enum {EnumName} : {UnderlyingType} {{ {valuesStr} }} (line {LineNumber} in {FilePath})";
    }
}

