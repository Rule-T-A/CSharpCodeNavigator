using CodeAnalyzer.Api.Models;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for retrieving detailed information about code elements (methods, classes).
/// </summary>
public interface ICodeElementService
{
    /// <summary>
    /// Gets detailed information about a method by its fully qualified name.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="methodFqn">Fully qualified method name (e.g., "Namespace.ClassName.MethodName")</param>
    /// <returns>Detailed method information</returns>
    Task<MethodDetailResponse> GetMethodAsync(string projectId, string methodFqn);

    /// <summary>
    /// Gets detailed information about a class by its fully qualified name.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="classFqn">Fully qualified class name (e.g., "Namespace.ClassName")</param>
    /// <returns>Detailed class information</returns>
    Task<ClassDetailResponse> GetClassAsync(string projectId, string classFqn);

    /// <summary>
    /// Gets all methods in a class.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="classFqn">Fully qualified class name (e.g., "Namespace.ClassName")</param>
    /// <returns>List of methods in the class</returns>
    Task<ClassMethodsResponse> GetClassMethodsAsync(string projectId, string classFqn);
}

