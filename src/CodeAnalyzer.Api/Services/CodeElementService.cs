using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Roslyn.Tests;
using Microsoft.Extensions.Logging;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for retrieving detailed information about code elements (methods, classes).
/// </summary>
public class CodeElementService : ICodeElementService
{
    private readonly IProjectManager _projectManager;
    private readonly ILogger<CodeElementService>? _logger;

    /// <summary>
    /// Creates a new instance of CodeElementService.
    /// </summary>
    /// <param name="projectManager">Project manager for accessing project information</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public CodeElementService(IProjectManager projectManager, ILogger<CodeElementService>? logger = null)
    {
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MethodDetailResponse> GetMethodAsync(string projectId, string methodFqn)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(methodFqn))
            throw new ArgumentException("Method FQN is required", nameof(methodFqn));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        FileVectorStoreAdapter? vectorStore = null;

        try
        {
            vectorStore = await FileVectorStoreAdapter.CreateAsync(project.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);

            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" &&
                    metadata.ContainsKey("method") && metadata["method"]?.ToString() == methodFqn)
                {
                    return new MethodDetailResponse
                    {
                        FullyQualifiedName = metadata.ContainsKey("method") ? metadata["method"]?.ToString() ?? string.Empty : string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsVirtual = metadata.ContainsKey("is_virtual") && bool.TryParse(metadata["is_virtual"]?.ToString(), out var isVirtual) && isVirtual,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsOverride = metadata.ContainsKey("is_override") && bool.TryParse(metadata["is_override"]?.ToString(), out var isOverride) && isOverride,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var lineNumber) ? lineNumber : 0
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting method {MethodFqn} for project {ProjectId}", methodFqn, projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }

        throw new KeyNotFoundException($"Method '{methodFqn}' not found in project '{projectId}'");
    }

    /// <inheritdoc/>
    public async Task<ClassDetailResponse> GetClassAsync(string projectId, string classFqn)
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
            var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);

            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" &&
                    metadata.ContainsKey("class") && metadata["class"]?.ToString() == classFqn)
                {
                    return new ClassDetailResponse
                    {
                        FullyQualifiedName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class_name") ? metadata["class_name"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty,
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic,
                        IsAbstract = metadata.ContainsKey("is_abstract") && bool.TryParse(metadata["is_abstract"]?.ToString(), out var isAbstract) && isAbstract,
                        IsSealed = metadata.ContainsKey("is_sealed") && bool.TryParse(metadata["is_sealed"]?.ToString(), out var isSealed) && isSealed,
                        BaseClass = metadata.ContainsKey("base_class") ? metadata["base_class"]?.ToString() : null,
                        Interfaces = metadata.ContainsKey("interfaces") && metadata["interfaces"]?.ToString() is string interfacesStr
                            ? interfacesStr.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList()
                            : new List<string>(),
                        MethodCount = metadata.ContainsKey("method_count") && int.TryParse(metadata["method_count"]?.ToString(), out var methodCount) ? methodCount : 0,
                        PropertyCount = metadata.ContainsKey("property_count") && int.TryParse(metadata["property_count"]?.ToString(), out var propertyCount) ? propertyCount : 0,
                        FieldCount = metadata.ContainsKey("field_count") && int.TryParse(metadata["field_count"]?.ToString(), out var fieldCount) ? fieldCount : 0,
                        FilePath = metadata.ContainsKey("file_path") ? metadata["file_path"]?.ToString() ?? string.Empty : string.Empty,
                        LineNumber = metadata.ContainsKey("line_number") && int.TryParse(metadata["line_number"]?.ToString(), out var lineNumber) ? lineNumber : 0
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class {ClassFqn} for project {ProjectId}", classFqn, projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }

        throw new KeyNotFoundException($"Class '{classFqn}' not found in project '{projectId}'");
    }

    /// <inheritdoc/>
    public async Task<ClassMethodsResponse> GetClassMethodsAsync(string projectId, string classFqn)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(classFqn))
            throw new ArgumentException("Class FQN is required", nameof(classFqn));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        var methods = new List<MethodInfo>();
        FileVectorStoreAdapter? vectorStore = null;

        try
        {
            vectorStore = await FileVectorStoreAdapter.CreateAsync(project.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);

            // First verify the class exists
            bool classExists = false;
            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" &&
                    metadata.ContainsKey("class") && metadata["class"]?.ToString() == classFqn)
                {
                    classExists = true;
                    break;
                }
            }

            if (!classExists)
            {
                throw new KeyNotFoundException($"Class '{classFqn}' not found in project '{projectId}'");
            }

            // Now find all methods in this class
            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition" &&
                    metadata.ContainsKey("class") && metadata["class"]?.ToString() == classFqn)
                {
                    var methodNamespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty;
                    var methodClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;
                    
                    // Extract just the class name from FQN (Namespace.ClassName)
                    var simpleClassName = methodClassName.Contains('.') 
                        ? methodClassName.Substring(methodClassName.LastIndexOf('.') + 1)
                        : methodClassName;

                    var methodInfo = new MethodInfo
                    {
                        FullyQualifiedName = metadata.ContainsKey("method") ? metadata["method"]?.ToString() ?? string.Empty : string.Empty,
                        MethodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = simpleClassName,
                        Namespace = methodNamespace,
                        ReturnType = metadata.ContainsKey("return_type") ? metadata["return_type"]?.ToString() ?? string.Empty : string.Empty,
                        Parameters = metadata.ContainsKey("parameters") && metadata["parameters"]?.ToString() is string paramsStr
                            ? paramsStr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList()
                            : new List<string>(),
                        AccessModifier = metadata.ContainsKey("access_modifier") ? metadata["access_modifier"]?.ToString() ?? string.Empty : string.Empty,
                        IsStatic = metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic
                    };

                    methods.Add(methodInfo);
                }
            }
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class methods for class {ClassFqn} in project {ProjectId}", classFqn, projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }

        return new ClassMethodsResponse
        {
            ClassFullyQualifiedName = classFqn,
            Methods = methods,
            TotalCount = methods.Count
        };
    }
}

