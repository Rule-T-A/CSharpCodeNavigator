namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents a property definition discovered during code analysis.
/// Contains all metadata needed to store the property definition in the vector database.
/// </summary>
public class PropertyDefinitionInfo
{
    /// <summary>
    /// Name of the property
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the property
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the property
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the property
    /// Format: Namespace.ClassName.PropertyName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the property
    /// </summary>
    public string PropertyType { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier of the property (public, private, protected, internal)
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the property is static
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether the property is virtual
    /// </summary>
    public bool IsVirtual { get; set; }

    /// <summary>
    /// Whether the property is abstract
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Whether the property is an override
    /// </summary>
    public bool IsOverride { get; set; }

    /// <summary>
    /// Whether the property has a getter
    /// </summary>
    public bool HasGetter { get; set; }

    /// <summary>
    /// Whether the property has a setter
    /// </summary>
    public bool HasSetter { get; set; }

    /// <summary>
    /// Whether the property is auto-implemented (has no explicit accessors)
    /// </summary>
    public bool IsAutoProperty { get; set; }

    /// <summary>
    /// Path to the source file containing the property definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the property definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of PropertyDefinitionInfo
    /// </summary>
    public PropertyDefinitionInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of PropertyDefinitionInfo with the specified values
    /// </summary>
    public PropertyDefinitionInfo(
        string propertyName,
        string className,
        string namespaceName,
        string fullyQualifiedName,
        string propertyType,
        string accessModifier,
        bool isStatic,
        bool isVirtual,
        bool isAbstract,
        bool isOverride,
        bool hasGetter,
        bool hasSetter,
        bool isAutoProperty,
        string filePath,
        int lineNumber)
    {
        PropertyName = propertyName;
        ClassName = className;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        PropertyType = propertyType;
        AccessModifier = accessModifier;
        IsStatic = isStatic;
        IsVirtual = isVirtual;
        IsAbstract = isAbstract;
        IsOverride = isOverride;
        HasGetter = hasGetter;
        HasSetter = hasSetter;
        IsAutoProperty = isAutoProperty;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the property definition
    /// </summary>
    public override string ToString()
    {
        var staticStr = IsStatic ? "static " : "";
        var virtualStr = IsVirtual ? "virtual " : "";
        var abstractStr = IsAbstract ? "abstract " : "";
        var overrideStr = IsOverride ? "override " : "";
        
        var accessors = "";
        if (HasGetter && HasSetter)
            accessors = "{ get; set; }";
        else if (HasGetter)
            accessors = "{ get; }";
        else if (HasSetter)
            accessors = "{ set; }";
        else
            accessors = "{ }";
        
        return $"{AccessModifier} {staticStr}{virtualStr}{abstractStr}{overrideStr}{PropertyType} {PropertyName} {accessors} (line {LineNumber} in {FilePath})";
    }
}

