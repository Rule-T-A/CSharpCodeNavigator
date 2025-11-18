using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;
using VectorStore.Core;
using Microsoft.Extensions.Logging;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for managing project indexing and metadata.
/// </summary>
public class ProjectManager : IProjectManager
{
    private readonly Dictionary<string, ProjectInfo> _projects = new();
    private readonly Dictionary<string, ProjectStatus> _statuses = new();
    private readonly Dictionary<string, Task> _indexingTasks = new();
    private readonly string _baseVectorStorePath;
    private readonly ILogger<ProjectManager>? _logger;

    /// <summary>
    /// Creates a new instance of ProjectManager.
    /// </summary>
    /// <param name="baseVectorStorePath">Base directory for storing vector stores</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public ProjectManager(string baseVectorStorePath = "./vector-stores", ILogger<ProjectManager>? logger = null)
    {
        _baseVectorStorePath = baseVectorStorePath;
        _logger = logger;

        // Ensure base directory exists
        if (!Directory.Exists(_baseVectorStorePath))
        {
            Directory.CreateDirectory(_baseVectorStorePath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> IndexProjectAsync(string projectPath, string? projectName = null)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required", nameof(projectPath));

        // Normalize project path
        projectPath = Path.GetFullPath(projectPath);

        // Generate project ID
        var projectId = GenerateProjectId(projectPath);

        // Check if project already exists
        if (_projects.ContainsKey(projectId))
        {
            _logger?.LogInformation("Project {ProjectId} already exists, returning existing ID", projectId);
            return projectId;
        }

        // Determine project name
        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = Path.GetFileNameWithoutExtension(projectPath);
            if (string.IsNullOrWhiteSpace(projectName))
            {
                projectName = Path.GetFileName(projectPath);
            }
        }

        // Create project info
        var projectInfo = new ProjectInfo
        {
            ProjectId = projectId,
            ProjectName = projectName,
            ProjectPath = projectPath,
            VectorStorePath = Path.Combine(_baseVectorStorePath, projectId),
            CreatedAt = DateTime.UtcNow
        };

        // Create initial status
        var status = new ProjectStatus
        {
            ProjectId = projectId,
            Status = IndexingStatus.Queued,
            Progress = 0,
            Message = "Project queued for indexing",
            StartedAt = DateTime.UtcNow
        };

        _projects[projectId] = projectInfo;
        _statuses[projectId] = status;

        // Start indexing asynchronously
        var indexingTask = Task.Run(async () => await IndexProjectInternalAsync(projectId, projectPath).ConfigureAwait(false));
        _indexingTasks[projectId] = indexingTask;

        _logger?.LogInformation("Project {ProjectId} ({ProjectName}) queued for indexing", projectId, projectName);

        return projectId;
    }

    /// <inheritdoc/>
    public Task<ProjectStatus> GetProjectStatusAsync(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));

        if (!_statuses.TryGetValue(projectId, out var status))
        {
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");
        }

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task<List<ProjectInfo>> ListProjectsAsync()
    {
        return Task.FromResult(_projects.Values.ToList());
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteProjectAsync(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));

        if (!_projects.TryGetValue(projectId, out var projectInfo))
        {
            return false;
        }

        try
        {
            // Wait for any ongoing indexing to complete or cancel
            if (_indexingTasks.TryGetValue(projectId, out var indexingTask))
            {
                // Note: We don't cancel the task, just wait for it to finish
                // In a production system, you'd want proper cancellation support
                try
                {
                    await indexingTask.ConfigureAwait(false);
                }
                catch
                {
                    // Ignore errors from indexing task
                }
                _indexingTasks.Remove(projectId);
            }

            // Delete vector store directory
            if (Directory.Exists(projectInfo.VectorStorePath))
            {
                try
                {
                    await FileVectorStore.DeleteAsync(projectInfo.VectorStorePath).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete vector store for project {ProjectId}", projectId);
                    // Continue with deletion even if vector store deletion fails
                }
            }

            // Remove from dictionaries
            _projects.Remove(projectId);
            _statuses.Remove(projectId);

            _logger?.LogInformation("Project {ProjectId} deleted successfully", projectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting project {ProjectId}", projectId);
            return false;
        }
    }

    /// <summary>
    /// Internal method to perform the actual indexing.
    /// </summary>
    private async Task IndexProjectInternalAsync(string projectId, string projectPath)
    {
        var status = _statuses[projectId];
        var projectInfo = _projects[projectId];

        try
        {
            status.Status = IndexingStatus.Indexing;
            status.Progress = 10;
            status.Message = "Initializing vector store...";
            _logger?.LogInformation("Starting indexing for project {ProjectId}", projectId);

            // Create vector store adapter
            FileVectorStoreAdapter? vectorStore = null;
            try
            {
                vectorStore = await FileVectorStoreAdapter.CreateAsync(projectInfo.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                status.Status = IndexingStatus.Failed;
                status.Progress = 0;
                status.Message = $"Failed to initialize vector store: {ex.Message}";
                status.Errors.Add(ex.Message);
                status.CompletedAt = DateTime.UtcNow;
                _logger?.LogError(ex, "Failed to initialize vector store for project {ProjectId}", projectId);
                return;
            }

            try
            {
                status.Progress = 20;
                status.Message = "Analyzing project...";

                // Create analyzer with vector store
                var analyzer = new RoslynAnalyzer(vectorStore);

                // Analyze project
                var result = await analyzer.AnalyzeProjectAsync(projectPath).ConfigureAwait(false);

                // Update project info with results
                projectInfo.FilesProcessed = result.FilesProcessed;
                projectInfo.MethodsAnalyzed = result.MethodsAnalyzed;
                projectInfo.MethodCallCount = result.MethodCallCount;
                projectInfo.MethodDefinitionCount = result.MethodDefinitionCount;
                projectInfo.ClassDefinitionCount = result.ClassDefinitionCount;
                projectInfo.PropertyDefinitionCount = result.PropertyDefinitionCount;
                projectInfo.FieldDefinitionCount = result.FieldDefinitionCount;
                projectInfo.EnumDefinitionCount = result.EnumDefinitionCount;
                projectInfo.InterfaceDefinitionCount = result.InterfaceDefinitionCount;
                projectInfo.StructDefinitionCount = result.StructDefinitionCount;
                projectInfo.IndexedAt = DateTime.UtcNow;

                // Update status
                if (result.Errors.Count > 0)
                {
                    status.Status = IndexingStatus.Completed; // Still completed, but with warnings
                    status.Message = $"Indexing completed with {result.Errors.Count} warning(s)";
                    status.Errors.AddRange(result.Errors);
                }
                else
                {
                    status.Status = IndexingStatus.Completed;
                    status.Message = "Indexing completed successfully";
                }

                status.Progress = 100;
                status.CompletedAt = DateTime.UtcNow;

                _logger?.LogInformation(
                    "Indexing completed for project {ProjectId}: {FilesProcessed} files, {MethodsAnalyzed} methods, {MethodCallCount} calls",
                    projectId, result.FilesProcessed, result.MethodsAnalyzed, result.MethodCallCount);
            }
            catch (Exception ex)
            {
                status.Status = IndexingStatus.Failed;
                status.Progress = 0;
                status.Message = $"Indexing failed: {ex.Message}";
                status.Errors.Add(ex.Message);
                status.CompletedAt = DateTime.UtcNow;
                _logger?.LogError(ex, "Indexing failed for project {ProjectId}", projectId);
            }
            finally
            {
                // Dispose vector store
                vectorStore?.Dispose();
            }
        }
        catch (Exception ex)
        {
            status.Status = IndexingStatus.Failed;
            status.Progress = 0;
            status.Message = $"Unexpected error: {ex.Message}";
            status.Errors.Add(ex.Message);
            status.CompletedAt = DateTime.UtcNow;
            _logger?.LogError(ex, "Unexpected error during indexing for project {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// Generates a unique project ID from the project path.
    /// </summary>
    private static string GenerateProjectId(string projectPath)
    {
        // Use a hash of the full path to ensure uniqueness
        var hash = projectPath.GetHashCode();
        var normalizedPath = projectPath.Replace('\\', '/').ToLowerInvariant();
        var pathHash = normalizedPath.GetHashCode();
        
        // Combine both hashes for better uniqueness
        var combinedHash = HashCode.Combine(hash, pathHash);
        
        // Convert to positive hex string
        return Math.Abs(combinedHash).ToString("X8");
    }
}

