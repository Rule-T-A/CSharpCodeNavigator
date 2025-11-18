using CodeAnalyzer.Api.Models;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for managing project indexing and metadata.
/// </summary>
public interface IProjectManager
{
    /// <summary>
    /// Indexes a C# project and stores the results in a vector store.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file or project directory</param>
    /// <param name="projectName">Optional display name for the project</param>
    /// <returns>Project ID and initial status</returns>
    Task<string> IndexProjectAsync(string projectPath, string? projectName = null);

    /// <summary>
    /// Gets the current status of a project indexing operation.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <returns>Current project status</returns>
    Task<ProjectStatus> GetProjectStatusAsync(string projectId);

    /// <summary>
    /// Lists all indexed projects.
    /// </summary>
    /// <returns>List of project information</returns>
    Task<List<ProjectInfo>> ListProjectsAsync();

    /// <summary>
    /// Deletes a project and its associated vector store.
    /// </summary>
    /// <param name="projectId">Unique identifier for the project</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteProjectAsync(string projectId);
}

