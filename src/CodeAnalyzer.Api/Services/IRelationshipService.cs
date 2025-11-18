using CodeAnalyzer.Api.Models;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for traversing code relationships (callers, callees, class references).
/// </summary>
public interface IRelationshipService
{
    /// <summary>
    /// Gets all methods that call the specified method (callers).
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="methodFqn">Fully qualified method name (e.g., "Namespace.ClassName.MethodName")</param>
    /// <param name="depth">Maximum depth to traverse (1 = direct callers only, 2 = callers of callers, etc.)</param>
    /// <param name="includeSelf">Whether to include the method itself in results</param>
    /// <returns>List of callers with depth information</returns>
    Task<CallersResponse> GetCallersAsync(string projectId, string methodFqn, int depth = 1, bool includeSelf = false);

    /// <summary>
    /// Gets all methods called by the specified method (callees).
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="methodFqn">Fully qualified method name (e.g., "Namespace.ClassName.MethodName")</param>
    /// <param name="depth">Maximum depth to traverse (1 = direct callees only, 2 = callees of callees, etc.)</param>
    /// <param name="includeSelf">Whether to include the method itself in results</param>
    /// <returns>List of callees with depth information</returns>
    Task<CalleesResponse> GetCalleesAsync(string projectId, string methodFqn, int depth = 1, bool includeSelf = false);

    /// <summary>
    /// Gets all classes that reference the specified class.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="classFqn">Fully qualified class name (e.g., "Namespace.ClassName")</param>
    /// <param name="relationshipType">Optional filter for relationship type (e.g., "calls", "inherits", "implements")</param>
    /// <returns>List of class references</returns>
    Task<ClassReferencesResponse> GetClassReferencesAsync(string projectId, string classFqn, string? relationshipType = null);
}

