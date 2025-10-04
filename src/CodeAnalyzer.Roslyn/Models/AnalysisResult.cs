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
               $"{MethodsAnalyzed} methods analyzed, " +
               $"{FilesProcessed} files processed, " +
               $"{Errors.Count} errors";
    }
}
