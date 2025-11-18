using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Api.Services;
using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CodeAnalyzer.Api.Tests.Services;

/// <summary>
/// Tests for EnumerationService.
/// </summary>
public class EnumerationServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testVectorStoreBasePath;
    private readonly ILogger<ProjectManager> _projectLogger;
    private readonly ILogger<EnumerationService> _enumerationLogger;
    private readonly ProjectManager _projectManager;
    private readonly EnumerationService _enumerationService;

    public EnumerationServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _testVectorStoreBasePath = Path.Combine(Path.GetTempPath(), $"test-enum-vector-stores-{Guid.NewGuid()}");
        _projectLogger = new TestLogger<ProjectManager>(output);
        _enumerationLogger = new TestLogger<EnumerationService>(output);
        _projectManager = new ProjectManager(_testVectorStoreBasePath, _projectLogger);
        _enumerationService = new EnumerationService(_projectManager, _enumerationLogger);

        // Ensure base directory exists
        if (!Directory.Exists(_testVectorStoreBasePath))
        {
            Directory.CreateDirectory(_testVectorStoreBasePath);
        }
    }

    public void Dispose()
    {
        // Clean up test vector stores
        if (Directory.Exists(_testVectorStoreBasePath))
        {
            try
            {
                Directory.Delete(_testVectorStoreBasePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void EnumerationService_Can_Be_Instantiated()
    {
        // Assert
        Assert.NotNull(_enumerationService);
    }

    [Fact]
    public async Task ListClassesAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _enumerationService.ListClassesAsync("invalid-id"));
    }

    [Fact]
    public async Task ListClassesAsync_Returns_Results_For_Indexed_Project()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var result = await _enumerationService.ListClassesAsync(projectId);

        // Assert
        Assert.NotNull(result);
        // The Roslyn project should have at least one class (RoslynAnalyzer)
        Assert.True(result.TotalCount > 0, "Expected at least one class in the Roslyn project");
    }

    [Fact]
    public async Task ListMethodsAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _enumerationService.ListMethodsAsync("invalid-id"));
    }

    [Fact]
    public async Task ListMethodsAsync_Returns_Results_For_Indexed_Project()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var result = await _enumerationService.ListMethodsAsync(projectId);

        // Assert
        Assert.NotNull(result);
        // The Roslyn project should have at least one method
        Assert.True(result.TotalCount > 0, "Expected at least one method in the Roslyn project");
    }

    [Fact]
    public async Task ListEntryPointsAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _enumerationService.ListEntryPointsAsync("invalid-id"));
    }

    [Fact]
    public async Task ListEntryPointsAsync_Returns_Results_For_Indexed_Project()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var result = await _enumerationService.ListEntryPointsAsync(projectId);

        // Assert
        Assert.NotNull(result);
        // Entry points may or may not exist depending on the project
        Assert.True(result.TotalCount >= 0, "Entry points count should be non-negative");
    }

    [Fact]
    public async Task ListClassesAsync_With_Pagination_Respects_Limit_And_Offset()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var result1 = await _enumerationService.ListClassesAsync(projectId, limit: 2, offset: 0);
        var result2 = await _enumerationService.ListClassesAsync(projectId, limit: 2, offset: 2);

        // Assert
        Assert.True(result1.Count <= 2);
        Assert.True(result2.Count <= 2);
        Assert.True(result1.TotalCount >= result1.Count);
    }

    [Fact]
    public async Task ListMethodsAsync_With_Pagination_Respects_Limit_And_Offset()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var result1 = await _enumerationService.ListMethodsAsync(projectId, limit: 5, offset: 0);
        var result2 = await _enumerationService.ListMethodsAsync(projectId, limit: 5, offset: 5);

        // Assert
        Assert.True(result1.Count <= 5);
        Assert.True(result2.Count <= 5);
        Assert.True(result1.TotalCount >= result1.Count);
    }

    [Fact]
    public async Task ListClassesAsync_With_Namespace_Filter_Filters_Correctly()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var allClasses = await _enumerationService.ListClassesAsync(projectId);
        if (allClasses.TotalCount > 0)
        {
            var firstNamespace = allClasses.Classes.First().Namespace;
            var filteredClasses = await _enumerationService.ListClassesAsync(projectId, @namespace: firstNamespace);

            // Assert
            Assert.All(filteredClasses.Classes, c => Assert.Equal(firstNamespace, c.Namespace));
        }
    }

    [Fact]
    public async Task ListMethodsAsync_With_Class_Filter_Filters_Correctly()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act
        var allMethods = await _enumerationService.ListMethodsAsync(projectId);
        if (allMethods.TotalCount > 0)
        {
            var firstClassName = allMethods.Methods.First().ClassName;
            var filteredMethods = await _enumerationService.ListMethodsAsync(projectId, className: firstClassName);

            // Assert
            Assert.All(filteredMethods.Methods, m => Assert.Equal(firstClassName, m.ClassName));
        }
    }

    /// <summary>
    /// Waits for project indexing to complete.
    /// </summary>
    private async Task WaitForIndexingComplete(string projectId, int maxWaitSeconds = 30)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
        {
            var status = await _projectManager.GetProjectStatusAsync(projectId);
            if (status.Status == IndexingStatus.Completed || status.Status == IndexingStatus.Failed)
            {
                return;
            }
            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Gets a path to a test project.
    /// </summary>
    private static string GetTestProjectPath()
    {
        var solutionDir = FindSolutionDirectory();
        var roslynProjectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        
        if (File.Exists(roslynProjectPath))
        {
            return roslynProjectPath;
        }

        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Finds the solution directory by looking for .sln file.
    /// </summary>
    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Simple test logger that writes to test output.
    /// </summary>
    private class TestLogger<T> : ILogger<T>
    {
        private readonly ITestOutputHelper _output;

        public TestLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _output.WriteLine($"[{logLevel}] {message}");
        }
    }
}

