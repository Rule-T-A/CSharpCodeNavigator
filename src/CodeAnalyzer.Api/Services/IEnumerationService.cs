using CodeAnalyzer.Api.Models;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for enumerating code elements from indexed projects.
/// </summary>
public interface IEnumerationService
{
    /// <summary>
    /// Lists classes in a project with optional filtering and pagination.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="offset">Number of results to skip</param>
    /// <returns>List of classes matching the criteria</returns>
    Task<ClassListResponse> ListClassesAsync(string projectId, string? @namespace = null, int limit = 100, int offset = 0);

    /// <summary>
    /// Lists methods in a project with optional filtering and pagination.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="className">Optional class name filter</param>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="offset">Number of results to skip</param>
    /// <returns>List of methods matching the criteria</returns>
    Task<MethodListResponse> ListMethodsAsync(string projectId, string? className = null, string? @namespace = null, int limit = 100, int offset = 0);

    /// <summary>
    /// Lists entry points (Main methods, controller actions) in a project.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <param name="type">Optional type filter (e.g., "Main", "Controller")</param>
    /// <returns>List of entry points matching the criteria</returns>
    Task<EntryPointListResponse> ListEntryPointsAsync(string projectId, string? type = null);
}

