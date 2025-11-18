namespace CodeAnalyzer.Api.Models;

/// <summary>
/// Represents metadata about an indexed project.
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// Unique identifier for the project.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Path to the project file (.csproj) or project directory.
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the vector store for this project.
    /// </summary>
    public string VectorStorePath { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the project was indexed.
    /// </summary>
    public DateTime IndexedAt { get; set; }

    /// <summary>
    /// Timestamp when the project was created/added.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of files processed during indexing.
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Number of methods analyzed during indexing.
    /// </summary>
    public int MethodsAnalyzed { get; set; }

    /// <summary>
    /// Number of method calls found.
    /// </summary>
    public int MethodCallCount { get; set; }

    /// <summary>
    /// Number of method definitions found.
    /// </summary>
    public int MethodDefinitionCount { get; set; }

    /// <summary>
    /// Number of class definitions found.
    /// </summary>
    public int ClassDefinitionCount { get; set; }

    /// <summary>
    /// Number of property definitions found.
    /// </summary>
    public int PropertyDefinitionCount { get; set; }

    /// <summary>
    /// Number of field definitions found.
    /// </summary>
    public int FieldDefinitionCount { get; set; }

    /// <summary>
    /// Number of enum definitions found.
    /// </summary>
    public int EnumDefinitionCount { get; set; }

    /// <summary>
    /// Number of interface definitions found.
    /// </summary>
    public int InterfaceDefinitionCount { get; set; }

    /// <summary>
    /// Number of struct definitions found.
    /// </summary>
    public int StructDefinitionCount { get; set; }
}

