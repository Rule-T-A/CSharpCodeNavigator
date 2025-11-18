using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Roslyn.Tests;
using Microsoft.Extensions.Logging;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for traversing code relationships (callers, callees, class references).
/// </summary>
public class RelationshipService : IRelationshipService
{
    private readonly IProjectManager _projectManager;
    private readonly ILogger<RelationshipService>? _logger;

    /// <summary>
    /// Creates a new instance of RelationshipService.
    /// </summary>
    /// <param name="projectManager">Project manager for accessing project information</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public RelationshipService(IProjectManager projectManager, ILogger<RelationshipService>? logger = null)
    {
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CallersResponse> GetCallersAsync(string projectId, string methodFqn, int depth = 1, bool includeSelf = false)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(methodFqn))
            throw new ArgumentException("Method FQN is required", nameof(methodFqn));

        if (depth < 1)
            throw new ArgumentException("Depth must be at least 1", nameof(depth));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        FileVectorStoreAdapter? vectorStore = null;

        try
        {
            vectorStore = await FileVectorStoreAdapter.CreateAsync(project.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            
            // First verify the method exists
            if (!await MethodExistsAsync(vectorStore, methodFqn).ConfigureAwait(false))
            {
                throw new KeyNotFoundException($"Method '{methodFqn}' not found in project '{projectId}'");
            }

            var callers = new List<CallerInfo>();
            var visited = new HashSet<string>(); // Track visited methods to prevent infinite loops
            var currentLevel = new HashSet<string> { methodFqn };
            var maxDepthReached = 0;

            // If includeSelf is true, add the method itself at depth 0
            if (includeSelf)
            {
                var methodInfo = await GetMethodInfoAsync(vectorStore, methodFqn).ConfigureAwait(false);
                if (methodInfo != null)
                {
                    callers.Add(new CallerInfo
                    {
                        FullyQualifiedName = methodFqn,
                        MethodName = methodInfo.MethodName,
                        ClassName = methodInfo.ClassName,
                        Namespace = methodInfo.Namespace,
                        Depth = 0,
                        FilePath = methodInfo.FilePath,
                        LineNumber = methodInfo.LineNumber
                    });
                }
            }

            // Traverse call graph level by level
            for (int currentDepth = 1; currentDepth <= depth; currentDepth++)
            {
                var nextLevel = new HashSet<string>();
                maxDepthReached = currentDepth;

                foreach (var method in currentLevel)
                {
                    if (visited.Contains(method))
                        continue;

                    visited.Add(method);

                    var directCallers = await GetDirectCallersAsync(vectorStore, method).ConfigureAwait(false);
                    
                    foreach (var caller in directCallers)
                    {
                        if (visited.Contains(caller.FullyQualifiedName))
                            continue;

                        callers.Add(new CallerInfo
                        {
                            FullyQualifiedName = caller.FullyQualifiedName,
                            MethodName = caller.MethodName,
                            ClassName = caller.ClassName,
                            Namespace = caller.Namespace,
                            Depth = currentDepth,
                            FilePath = caller.FilePath,
                            LineNumber = caller.LineNumber
                        });

                        nextLevel.Add(caller.FullyQualifiedName);
                    }
                }

                if (nextLevel.Count == 0)
                    break; // No more callers to traverse

                currentLevel = nextLevel;
            }

            return new CallersResponse
            {
                MethodFullyQualifiedName = methodFqn,
                Callers = callers,
                TotalCount = callers.Count,
                MaxDepth = maxDepthReached
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting callers for method {MethodFqn} in project {ProjectId}", methodFqn, projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<CalleesResponse> GetCalleesAsync(string projectId, string methodFqn, int depth = 1, bool includeSelf = false)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(methodFqn))
            throw new ArgumentException("Method FQN is required", nameof(methodFqn));

        if (depth < 1)
            throw new ArgumentException("Depth must be at least 1", nameof(depth));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        FileVectorStoreAdapter? vectorStore = null;

        try
        {
            vectorStore = await FileVectorStoreAdapter.CreateAsync(project.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            
            // First verify the method exists
            if (!await MethodExistsAsync(vectorStore, methodFqn).ConfigureAwait(false))
            {
                throw new KeyNotFoundException($"Method '{methodFqn}' not found in project '{projectId}'");
            }

            var callees = new List<CalleeInfo>();
            var visited = new HashSet<string>(); // Track visited methods to prevent infinite loops
            var currentLevel = new HashSet<string> { methodFqn };
            var maxDepthReached = 0;

            // If includeSelf is true, add the method itself at depth 0
            if (includeSelf)
            {
                var methodInfo = await GetMethodInfoAsync(vectorStore, methodFqn).ConfigureAwait(false);
                if (methodInfo != null)
                {
                    callees.Add(new CalleeInfo
                    {
                        FullyQualifiedName = methodFqn,
                        MethodName = methodInfo.MethodName,
                        ClassName = methodInfo.ClassName,
                        Namespace = methodInfo.Namespace,
                        Depth = 0,
                        FilePath = methodInfo.FilePath,
                        LineNumber = methodInfo.LineNumber
                    });
                }
            }

            // Traverse call graph level by level
            for (int currentDepth = 1; currentDepth <= depth; currentDepth++)
            {
                var nextLevel = new HashSet<string>();
                maxDepthReached = currentDepth;

                foreach (var method in currentLevel)
                {
                    if (visited.Contains(method))
                        continue;

                    visited.Add(method);

                    var directCallees = await GetDirectCalleesAsync(vectorStore, method).ConfigureAwait(false);
                    
                    foreach (var callee in directCallees)
                    {
                        if (visited.Contains(callee.FullyQualifiedName))
                            continue;

                        callees.Add(new CalleeInfo
                        {
                            FullyQualifiedName = callee.FullyQualifiedName,
                            MethodName = callee.MethodName,
                            ClassName = callee.ClassName,
                            Namespace = callee.Namespace,
                            Depth = currentDepth,
                            FilePath = callee.FilePath,
                            LineNumber = callee.LineNumber
                        });

                        nextLevel.Add(callee.FullyQualifiedName);
                    }
                }

                if (nextLevel.Count == 0)
                    break; // No more callees to traverse

                currentLevel = nextLevel;
            }

            return new CalleesResponse
            {
                MethodFullyQualifiedName = methodFqn,
                Callees = callees,
                TotalCount = callees.Count,
                MaxDepth = maxDepthReached
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting callees for method {MethodFqn} in project {ProjectId}", methodFqn, projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<ClassReferencesResponse> GetClassReferencesAsync(string projectId, string classFqn, string? relationshipType = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(classFqn))
            throw new ArgumentException("Class FQN is required", nameof(classFqn));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        FileVectorStoreAdapter? vectorStore = null;

        try
        {
            vectorStore = await FileVectorStoreAdapter.CreateAsync(project.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            
            // First verify the class exists
            if (!await ClassExistsAsync(vectorStore, classFqn).ConfigureAwait(false))
            {
                throw new KeyNotFoundException($"Class '{classFqn}' not found in project '{projectId}'");
            }

            var references = new Dictionary<string, ClassReferenceInfo>(); // Use dictionary to deduplicate by class FQN

            // Find classes that call methods in the target class
            var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);
            
            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call")
                {
                    var calleeClass = metadata.ContainsKey("callee_class") ? metadata["callee_class"]?.ToString() ?? string.Empty : string.Empty;
                    var calleeNamespace = metadata.ContainsKey("callee_namespace") ? metadata["callee_namespace"]?.ToString() ?? string.Empty : string.Empty;
                    var calleeClassFqn = string.IsNullOrEmpty(calleeNamespace) ? calleeClass : $"{calleeNamespace}.{calleeClass}";

                    // Check if this call targets a method in the specified class
                    if (calleeClassFqn.Equals(classFqn, StringComparison.OrdinalIgnoreCase) ||
                        calleeClass.Equals(classFqn, StringComparison.OrdinalIgnoreCase))
                    {
                        var callerClass = metadata.ContainsKey("caller_class") ? metadata["caller_class"]?.ToString() ?? string.Empty : string.Empty;
                        var callerNamespace = metadata.ContainsKey("caller_namespace") ? metadata["caller_namespace"]?.ToString() ?? string.Empty : string.Empty;
                        var callerClassFqn = string.IsNullOrEmpty(callerNamespace) ? callerClass : $"{callerNamespace}.{callerClass}";

                        // Skip if caller is the same as target class
                        if (callerClassFqn.Equals(classFqn, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var relType = relationshipType ?? "calls";
                        
                        // Apply relationship type filter if specified
                        if (relationshipType != null && !relType.Equals(relationshipType, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!references.ContainsKey(callerClassFqn))
                        {
                            references[callerClassFqn] = new ClassReferenceInfo
                            {
                                FullyQualifiedName = callerClassFqn,
                                ClassName = callerClass,
                                Namespace = callerNamespace,
                                RelationshipType = relType,
                                FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                                LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var lineNumber) ? lineNumber : 0
                            };
                        }
                    }
                }
            }

            return new ClassReferencesResponse
            {
                ClassFullyQualifiedName = classFqn,
                References = references.Values.ToList(),
                TotalCount = references.Count
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class references for class {ClassFqn} in project {ProjectId}", classFqn, projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }
    }

    /// <summary>
    /// Checks if a method exists in the vector store.
    /// </summary>
    private async Task<bool> MethodExistsAsync(FileVectorStoreAdapter vectorStore, string methodFqn)
    {
        var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);
        
        foreach (var id in allIds)
        {
            var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
            if (doc == null) continue;

            var metadata = doc.Metadata;
            if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" &&
                metadata.ContainsKey("method") && metadata["method"]?.ToString() == methodFqn)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a class exists in the vector store.
    /// </summary>
    private async Task<bool> ClassExistsAsync(FileVectorStoreAdapter vectorStore, string classFqn)
    {
        var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);
        
        foreach (var id in allIds)
        {
            var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
            if (doc == null) continue;

            var metadata = doc.Metadata;
            if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" &&
                metadata.ContainsKey("class") && metadata["class"]?.ToString() == classFqn)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Internal helper class for method information.
    /// </summary>
    private record MethodInfoHelper(string MethodName, string ClassName, string Namespace, string FilePath, int LineNumber);

    /// <summary>
    /// Gets method information from the vector store.
    /// </summary>
    private async Task<MethodInfoHelper?> GetMethodInfoAsync(FileVectorStoreAdapter vectorStore, string methodFqn)
    {
        var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);
        
        foreach (var id in allIds)
        {
            var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
            if (doc == null) continue;

            var metadata = doc.Metadata;
            if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" &&
                metadata.ContainsKey("method") && metadata["method"]?.ToString() == methodFqn)
            {
                var methodClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;
                var simpleClassName = methodClassName.Contains('.') 
                    ? methodClassName.Substring(methodClassName.LastIndexOf('.') + 1)
                    : methodClassName;

                return new MethodInfoHelper(
                    MethodName: metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                    ClassName: simpleClassName,
                    Namespace: metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                    FilePath: metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                    LineNumber: metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var lineNumber) ? lineNumber : 0
                );
            }
        }

        return null;
    }

    /// <summary>
    /// Gets direct callers of a method (depth 1).
    /// </summary>
    private async Task<List<CallerInfo>> GetDirectCallersAsync(FileVectorStoreAdapter vectorStore, string methodFqn)
    {
        var callers = new List<CallerInfo>();
        var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);

        foreach (var id in allIds)
        {
            var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
            if (doc == null) continue;

            var metadata = doc.Metadata;
            if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                metadata.ContainsKey("callee") && metadata["callee"]?.ToString() == methodFqn)
            {
                var callerFqn = metadata.ContainsKey("caller") ? metadata["caller"]?.ToString() ?? string.Empty : string.Empty;
                var callerClass = metadata.ContainsKey("caller_class") ? metadata["caller_class"]?.ToString() ?? string.Empty : string.Empty;
                var callerNamespace = metadata.ContainsKey("caller_namespace") ? metadata["caller_namespace"]?.ToString() ?? string.Empty : string.Empty;

                // Extract method name from FQN
                var methodName = callerFqn.Contains('.') 
                    ? callerFqn.Substring(callerFqn.LastIndexOf('.') + 1)
                    : callerFqn;

                callers.Add(new CallerInfo
                {
                    FullyQualifiedName = callerFqn,
                    MethodName = methodName,
                    ClassName = callerClass,
                    Namespace = callerNamespace,
                    Depth = 1, // Will be overridden by caller
                    FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                    LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var lineNumber) ? lineNumber : 0
                });
            }
        }

        return callers;
    }

    /// <summary>
    /// Gets direct callees of a method (depth 1).
    /// </summary>
    private async Task<List<CalleeInfo>> GetDirectCalleesAsync(FileVectorStoreAdapter vectorStore, string methodFqn)
    {
        var callees = new List<CalleeInfo>();
        var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);

        foreach (var id in allIds)
        {
            var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
            if (doc == null) continue;

            var metadata = doc.Metadata;
            if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_call" &&
                metadata.ContainsKey("caller") && metadata["caller"]?.ToString() == methodFqn)
            {
                var calleeFqn = metadata.ContainsKey("callee") ? metadata["callee"]?.ToString() ?? string.Empty : string.Empty;
                var calleeClass = metadata.ContainsKey("callee_class") ? metadata["callee_class"]?.ToString() ?? string.Empty : string.Empty;
                var calleeNamespace = metadata.ContainsKey("callee_namespace") ? metadata["callee_namespace"]?.ToString() ?? string.Empty : string.Empty;

                // Extract method name from FQN
                var methodName = calleeFqn.Contains('.') 
                    ? calleeFqn.Substring(calleeFqn.LastIndexOf('.') + 1)
                    : calleeFqn;

                callees.Add(new CalleeInfo
                {
                    FullyQualifiedName = calleeFqn,
                    MethodName = methodName,
                    ClassName = calleeClass,
                    Namespace = calleeNamespace,
                    Depth = 1, // Will be overridden by caller
                    FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                    LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var lineNumber) ? lineNumber : 0
                });
            }
        }

        return callees;
    }
}

