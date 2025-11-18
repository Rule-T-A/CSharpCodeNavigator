namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Contains the results of analyzing a C# project for method call relationships.
/// Aggregates all discovered method calls and provides metadata about the analysis process.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// All method call relationships discovered during analysis
    /// </summary>
    public List<MethodCallInfo> MethodCalls { get; set; } = new();

    /// <summary>
    /// All method definitions discovered during analysis
    /// </summary>
    public List<MethodDefinitionInfo> MethodDefinitions { get; set; } = new();

    /// <summary>
    /// All class definitions discovered during analysis
    /// </summary>
    public List<ClassDefinitionInfo> ClassDefinitions { get; set; } = new();

    /// <summary>
    /// All property definitions discovered during analysis
    /// </summary>
    public List<PropertyDefinitionInfo> PropertyDefinitions { get; set; } = new();

    /// <summary>
    /// All field definitions discovered during analysis
    /// </summary>
    public List<FieldDefinitionInfo> FieldDefinitions { get; set; } = new();

    /// <summary>
    /// All enum definitions discovered during analysis
    /// </summary>
    public List<EnumDefinitionInfo> EnumDefinitions { get; set; } = new();

    /// <summary>
    /// All interface definitions discovered during analysis
    /// </summary>
    public List<InterfaceDefinitionInfo> InterfaceDefinitions { get; set; } = new();

    /// <summary>
    /// All struct definitions discovered during analysis
    /// </summary>
    public List<StructDefinitionInfo> StructDefinitions { get; set; } = new();

    /// <summary>
    /// Number of methods that were analyzed
    /// </summary>
    public int MethodsAnalyzed { get; set; }

    /// <summary>
    /// Number of source files that were processed
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Any errors encountered during analysis
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Total number of method call relationships found
    /// </summary>
    public int MethodCallCount => MethodCalls.Count;

    /// <summary>
    /// Total number of method definitions found
    /// </summary>
    public int MethodDefinitionCount => MethodDefinitions.Count;

    /// <summary>
    /// Total number of class definitions found
    /// </summary>
    public int ClassDefinitionCount => ClassDefinitions.Count;

    /// <summary>
    /// Total number of property definitions found
    /// </summary>
    public int PropertyDefinitionCount => PropertyDefinitions.Count;

    /// <summary>
    /// Total number of field definitions found
    /// </summary>
    public int FieldDefinitionCount => FieldDefinitions.Count;

    /// <summary>
    /// Total number of enum definitions found
    /// </summary>
    public int EnumDefinitionCount => EnumDefinitions.Count;

    /// <summary>
    /// Total number of interface definitions found
    /// </summary>
    public int InterfaceDefinitionCount => InterfaceDefinitions.Count;

    /// <summary>
    /// Total number of struct definitions found
    /// </summary>
    public int StructDefinitionCount => StructDefinitions.Count;

    /// <summary>
    /// Whether the analysis completed successfully (no errors)
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    /// Creates a new instance of AnalysisResult
    /// </summary>
    public AnalysisResult()
    {
    }

    /// <summary>
    /// Creates a new instance of AnalysisResult with the specified values
    /// </summary>
    public AnalysisResult(
        List<MethodCallInfo> methodCalls,
        int methodsAnalyzed,
        int filesProcessed,
        List<string> errors)
    {
        MethodCalls = methodCalls ?? new List<MethodCallInfo>();
        MethodDefinitions = new List<MethodDefinitionInfo>();
        ClassDefinitions = new List<ClassDefinitionInfo>();
        PropertyDefinitions = new List<PropertyDefinitionInfo>();
        FieldDefinitions = new List<FieldDefinitionInfo>();
        EnumDefinitions = new List<EnumDefinitionInfo>();
        InterfaceDefinitions = new List<InterfaceDefinitionInfo>();
        StructDefinitions = new List<StructDefinitionInfo>();
        MethodsAnalyzed = methodsAnalyzed;
        FilesProcessed = filesProcessed;
        Errors = errors ?? new List<string>();
    }

    /// <summary>
    /// Adds a method call to the results
    /// </summary>
    public void AddMethodCall(MethodCallInfo methodCall)
    {
        if (methodCall != null)
        {
            MethodCalls.Add(methodCall);
        }
    }

    /// <summary>
    /// Adds a method definition to the results
    /// </summary>
    public void AddMethodDefinition(MethodDefinitionInfo methodDefinition)
    {
        if (methodDefinition != null)
        {
            MethodDefinitions.Add(methodDefinition);
        }
    }

    /// <summary>
    /// Adds a class definition to the results
    /// </summary>
    public void AddClassDefinition(ClassDefinitionInfo classDefinition)
    {
        if (classDefinition != null)
        {
            ClassDefinitions.Add(classDefinition);
        }
    }

    /// <summary>
    /// Adds a property definition to the results
    /// </summary>
    public void AddPropertyDefinition(PropertyDefinitionInfo propertyDefinition)
    {
        if (propertyDefinition != null)
        {
            PropertyDefinitions.Add(propertyDefinition);
        }
    }

    /// <summary>
    /// Adds a field definition to the results
    /// </summary>
    public void AddFieldDefinition(FieldDefinitionInfo fieldDefinition)
    {
        if (fieldDefinition != null)
        {
            FieldDefinitions.Add(fieldDefinition);
        }
    }

    /// <summary>
    /// Adds an enum definition to the results
    /// </summary>
    public void AddEnumDefinition(EnumDefinitionInfo enumDefinition)
    {
        if (enumDefinition != null)
        {
            EnumDefinitions.Add(enumDefinition);
        }
    }

    /// <summary>
    /// Adds an interface definition to the results
    /// </summary>
    public void AddInterfaceDefinition(InterfaceDefinitionInfo interfaceDefinition)
    {
        if (interfaceDefinition != null)
        {
            InterfaceDefinitions.Add(interfaceDefinition);
        }
    }

    /// <summary>
    /// Adds a struct definition to the results
    /// </summary>
    public void AddStructDefinition(StructDefinitionInfo structDefinition)
    {
        if (structDefinition != null)
        {
            StructDefinitions.Add(structDefinition);
        }
    }

    /// <summary>
    /// Adds an error to the results
    /// </summary>
    public void AddError(string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            Errors.Add(error);
        }
    }

    /// <summary>
    /// Adds multiple errors to the results
    /// </summary>
    public void AddErrors(IEnumerable<string> errors)
    {
        if (errors != null)
        {
            foreach (var error in errors)
            {
                AddError(error);
            }
        }
    }

    /// <summary>
    /// Returns a summary string of the analysis results
    /// </summary>
    public override string ToString()
    {
        return $"Analysis Result: {MethodCallCount} method calls found, " +
               $"{MethodDefinitionCount} method definitions found, " +
               $"{ClassDefinitionCount} class definitions found, " +
               $"{PropertyDefinitionCount} property definitions found, " +
               $"{FieldDefinitionCount} field definitions found, " +
               $"{EnumDefinitionCount} enum definitions found, " +
               $"{InterfaceDefinitionCount} interface definitions found, " +
               $"{StructDefinitionCount} struct definitions found, " +
               $"{MethodsAnalyzed} methods analyzed, " +
               $"{FilesProcessed} files processed, " +
               $"{Errors.Count} errors";
    }
}
