namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents a field definition discovered during code analysis.
/// Contains all metadata needed to store the field definition in the vector database.
/// </summary>
public class FieldDefinitionInfo
{
    /// <summary>
    /// Name of the field
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the field
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the field
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the field
    /// Format: Namespace.ClassName.FieldName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the field
    /// </summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier of the field (public, private, protected, internal)
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the field is static
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether the field is readonly
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the field is const
    /// </summary>
    public bool IsConst { get; set; }

    /// <summary>
    /// Whether the field is volatile
    /// </summary>
    public bool IsVolatile { get; set; }

    /// <summary>
    /// Path to the source file containing the field definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the field definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of FieldDefinitionInfo
    /// </summary>
    public FieldDefinitionInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of FieldDefinitionInfo with the specified values
    /// </summary>
    public FieldDefinitionInfo(
        string fieldName,
        string className,
        string namespaceName,
        string fullyQualifiedName,
        string fieldType,
        string accessModifier,
        bool isStatic,
        bool isReadOnly,
        bool isConst,
        bool isVolatile,
        string filePath,
        int lineNumber)
    {
        FieldName = fieldName;
        ClassName = className;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        FieldType = fieldType;
        AccessModifier = accessModifier;
        IsStatic = isStatic;
        IsReadOnly = isReadOnly;
        IsConst = isConst;
        IsVolatile = isVolatile;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the field definition
    /// </summary>
    public override string ToString()
    {
        var staticStr = IsStatic ? "static " : "";
        var readonlyStr = IsReadOnly ? "readonly " : "";
        var constStr = IsConst ? "const " : "";
        var volatileStr = IsVolatile ? "volatile " : "";
        
        return $"{AccessModifier} {staticStr}{readonlyStr}{constStr}{volatileStr}{FieldType} {FieldName} (line {LineNumber} in {FilePath})";
    }
}

