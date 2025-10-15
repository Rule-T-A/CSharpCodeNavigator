using System.Collections.Generic;

namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents a class definition discovered during code analysis.
/// </summary>
public class ClassDefinitionInfo
{
    /// <summary>
    /// Name of the class
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace of the class
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified name of the class
    /// Format: Namespace.ClassName
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Access modifier (e.g., "public", "private", "protected", "internal")
    /// </summary>
    public string AccessModifier { get; set; } = "private";

    /// <summary>
    /// True if the class is static
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// True if the class is abstract
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// True if the class is sealed
    /// </summary>
    public bool IsSealed { get; set; }

    /// <summary>
    /// Base class name (if any)
    /// </summary>
    public string BaseClass { get; set; } = string.Empty;

    /// <summary>
    /// List of implemented interfaces
    /// </summary>
    public List<string> Interfaces { get; set; } = new();

    /// <summary>
    /// Path to the source file containing the class definition
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the class definition starts (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Number of methods in the class
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of properties in the class
    /// </summary>
    public int PropertyCount { get; set; }

    /// <summary>
    /// Number of fields in the class
    /// </summary>
    public int FieldCount { get; set; }

    public ClassDefinitionInfo() { }

    public ClassDefinitionInfo(
        string className,
        string namespaceName,
        string fullyQualifiedName,
        string accessModifier,
        bool isStatic,
        bool isAbstract,
        bool isSealed,
        string baseClass,
        List<string> interfaces,
        string filePath,
        int lineNumber,
        int methodCount,
        int propertyCount,
        int fieldCount)
    {
        ClassName = className;
        Namespace = namespaceName;
        FullyQualifiedName = fullyQualifiedName;
        AccessModifier = accessModifier;
        IsStatic = isStatic;
        IsAbstract = isAbstract;
        IsSealed = isSealed;
        BaseClass = baseClass;
        Interfaces = interfaces ?? new List<string>();
        FilePath = filePath;
        LineNumber = lineNumber;
        MethodCount = methodCount;
        PropertyCount = propertyCount;
        FieldCount = fieldCount;
    }

    public override string ToString()
    {
        var modifiers = "";
        if (IsStatic) modifiers += "static ";
        if (IsAbstract) modifiers += "abstract ";
        if (IsSealed) modifiers += "sealed ";

        var inheritance = "";
        if (!string.IsNullOrEmpty(BaseClass))
        {
            inheritance += $" : {BaseClass}";
        }
        if (Interfaces.Count > 0)
        {
            inheritance += (string.IsNullOrEmpty(BaseClass) ? " : " : ", ") + string.Join(", ", Interfaces);
        }

        return $"{AccessModifier} {modifiers.Trim()}{(!string.IsNullOrEmpty(modifiers.Trim()) ? " " : "")}{ClassName}{inheritance} (line {LineNumber} in {FilePath})";
    }
}
