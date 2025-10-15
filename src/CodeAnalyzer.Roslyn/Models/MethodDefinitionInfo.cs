namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents a method definition discovered during code analysis.
/// Contains all metadata needed to store the method definition in the vector database.
/// </summary>
public class MethodDefinitionInfo
{
    /// <summary>
    /// Name of the method
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the method
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the method
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the method
    /// Format: Namespace.ClassName.MethodName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Return type of the method
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// List of parameter types (without parameter names)
    /// </summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>
    /// Access modifier of the method (public, private, protected, internal)
    /// </summary>
    public string AccessModifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the method is static
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Whether the method is virtual
    /// </summary>
    public bool IsVirtual { get; set; }

    /// <summary>
    /// Whether the method is abstract
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Whether the method is an override
    /// </summary>
    public bool IsOverride { get; set; }

    /// <summary>
    /// Path to the source file containing the method definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the method definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of MethodDefinitionInfo
    /// </summary>
    public MethodDefinitionInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of MethodDefinitionInfo with the specified values
    /// </summary>
    public MethodDefinitionInfo(
        string methodName,
        string className,
        string namespaceName,
        string fullyQualifiedName,
        string returnType,
        List<string> parameters,
        string accessModifier,
        bool isStatic,
        bool isVirtual,
        bool isAbstract,
        bool isOverride,
        string filePath,
        int lineNumber)
    {
        MethodName = methodName;
        ClassName = className;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        ReturnType = returnType;
        Parameters = parameters ?? new List<string>();
        AccessModifier = accessModifier;
        IsStatic = isStatic;
        IsVirtual = isVirtual;
        IsAbstract = isAbstract;
        IsOverride = isOverride;
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the method definition
    /// </summary>
    public override string ToString()
    {
        var parametersStr = string.Join(", ", Parameters);
        var staticStr = IsStatic ? "static " : "";
        var virtualStr = IsVirtual ? "virtual " : "";
        var abstractStr = IsAbstract ? "abstract " : "";
        var overrideStr = IsOverride ? "override " : "";
        
        return $"{AccessModifier} {staticStr}{virtualStr}{abstractStr}{overrideStr}{ReturnType} {MethodName}({parametersStr}) (line {LineNumber} in {FilePath})";
    }
}
