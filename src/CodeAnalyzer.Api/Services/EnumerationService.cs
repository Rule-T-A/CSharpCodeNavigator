using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Roslyn.Tests;
using VectorStore.Core;
using Microsoft.Extensions.Logging;

namespace CodeAnalyzer.Api.Services;

/// <summary>
/// Service for enumerating code elements from indexed projects.
/// </summary>
public class EnumerationService : IEnumerationService
{
    private readonly IProjectManager _projectManager;
    private readonly ILogger<EnumerationService>? _logger;

    /// <summary>
    /// Creates a new instance of EnumerationService.
    /// </summary>
    /// <param name="projectManager">Project manager for accessing project information</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public EnumerationService(IProjectManager projectManager, ILogger<EnumerationService>? logger = null)
    {
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ClassListResponse> ListClassesAsync(string projectId, string? @namespace = null, int limit = 100, int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        var classes = new List<ClassInfo>();
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
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition")
                {
                    var classNamespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty;
                    
                    // Apply namespace filter if specified
                    if (!string.IsNullOrWhiteSpace(@namespace) && !classNamespace.Equals(@namespace, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var classInfo = new ClassInfo
                    {
                        FullyQualifiedName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty,
                        ClassName = metadata.ContainsKey("class_name") ? metadata["class_name"]?.ToString() ?? string.Empty : string.Empty,
                        Namespace = classNamespace,
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
                        FieldCount = metadata.ContainsKey("field_count") && int.TryParse(metadata["field_count"]?.ToString(), out var fieldCount) ? fieldCount : 0
                    };

                    classes.Add(classInfo);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error listing classes for project {ProjectId}", projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }

        var totalCount = classes.Count;
        var paginatedClasses = classes.Skip(offset).Take(limit).ToList();

        return new ClassListResponse
        {
            Classes = paginatedClasses,
            TotalCount = totalCount,
            Count = paginatedClasses.Count,
            Offset = offset,
            Limit = limit
        };
    }

    /// <inheritdoc/>
    public async Task<MethodListResponse> ListMethodsAsync(string projectId, string? className = null, string? @namespace = null, int limit = 100, int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));

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

            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition")
                {
                    var methodNamespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty;
                    var methodClassName = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;
                    
                    // Extract just the class name from FQN (Namespace.ClassName)
                    var simpleClassName = methodClassName.Contains('.') 
                        ? methodClassName.Substring(methodClassName.LastIndexOf('.') + 1)
                        : methodClassName;

                    // Apply namespace filter if specified
                    if (!string.IsNullOrWhiteSpace(@namespace) && !methodNamespace.Equals(@namespace, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Apply class name filter if specified
                    if (!string.IsNullOrWhiteSpace(className) && !simpleClassName.Equals(className, StringComparison.OrdinalIgnoreCase))
                        continue;

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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error listing methods for project {ProjectId}", projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }

        var totalCount = methods.Count;
        var paginatedMethods = methods.Skip(offset).Take(limit).ToList();

        return new MethodListResponse
        {
            Methods = paginatedMethods,
            TotalCount = totalCount,
            Count = paginatedMethods.Count,
            Offset = offset,
            Limit = limit
        };
    }

    /// <inheritdoc/>
    public async Task<EntryPointListResponse> ListEntryPointsAsync(string projectId, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID is required", nameof(projectId));

        var projects = await _projectManager.ListProjectsAsync().ConfigureAwait(false);
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        if (project == null)
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found");

        var entryPoints = new List<EntryPointInfo>();
        FileVectorStoreAdapter? vectorStore = null;

        try
        {
            vectorStore = await FileVectorStoreAdapter.CreateAsync(project.VectorStorePath, VerbosityLevel.Terse).ConfigureAwait(false);
            var allIds = await vectorStore.Store.GetAllIdsAsync().ConfigureAwait(false);

            // First, collect all classes to check for controllers
            var classNames = new HashSet<string>();
            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "class_definition" &&
                    metadata.ContainsKey("class"))
                {
                    var className = metadata["class"]?.ToString() ?? string.Empty;
                    var simpleClassName = className.Contains('.') 
                        ? className.Substring(className.LastIndexOf('.') + 1)
                        : className;
                    
                    // Check if class name ends with "Controller" (ASP.NET Core convention)
                    if (simpleClassName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                    {
                        classNames.Add(className);
                    }
                }
            }

            // Now find entry point methods
            foreach (var id in allIds)
            {
                var doc = await vectorStore.Store.GetAsync(id).ConfigureAwait(false);
                if (doc == null) continue;

                var metadata = doc.Metadata;
                if (metadata.ContainsKey("type") && metadata["type"]?.ToString() == "method_definition")
                {
                    var methodName = metadata.ContainsKey("method_name") ? metadata["method_name"]?.ToString() ?? string.Empty : string.Empty;
                    var className = metadata.ContainsKey("class") ? metadata["class"]?.ToString() ?? string.Empty : string.Empty;
                    var methodNamespace = metadata.ContainsKey("namespace") ? metadata["namespace"]?.ToString() ?? string.Empty : string.Empty;

                    string? entryType = null;
                    string? httpMethod = null;
                    string? route = null;

                    // Check for Main method
                    if (methodName.Equals("Main", StringComparison.OrdinalIgnoreCase) && 
                        (methodName.Equals("Main", StringComparison.OrdinalIgnoreCase) || 
                         metadata.ContainsKey("is_static") && bool.TryParse(metadata["is_static"]?.ToString(), out var isStatic) && isStatic))
                    {
                        entryType = "Main";
                    }
                    // Check for controller actions
                    else if (classNames.Contains(className))
                    {
                        entryType = "Controller";
                        // Note: HTTP method and route detection would require parsing attributes from source code
                        // For now, we'll mark them as controller methods
                        httpMethod = "Unknown";
                        route = "Unknown";
                    }

                    if (entryType != null)
                    {
                        // Apply type filter if specified
                        if (!string.IsNullOrWhiteSpace(type) && !entryType.Equals(type, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var entryPoint = new EntryPointInfo
                        {
                            FullyQualifiedName = metadata.ContainsKey("method") ? metadata["method"]?.ToString() ?? string.Empty : string.Empty,
                            MethodName = methodName,
                            ClassName = className.Contains('.') 
                                ? className.Substring(className.LastIndexOf('.') + 1)
                                : className,
                            Namespace = methodNamespace,
                            Type = entryType,
                            HttpMethod = httpMethod,
                            Route = route
                        };

                        entryPoints.Add(entryPoint);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error listing entry points for project {ProjectId}", projectId);
            throw;
        }
        finally
        {
            vectorStore?.Dispose();
        }

        return new EntryPointListResponse
        {
            EntryPoints = entryPoints,
            TotalCount = entryPoints.Count
        };
    }
}

