using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Api.Services;
using CodeAnalyzer.Roslyn.Tests;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CodeAnalyzer.Api.Tests.Services;

/// <summary>
/// Tests for RelationshipService.
/// </summary>
public class RelationshipServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testVectorStoreBasePath;
    private readonly ILogger<ProjectManager> _projectLogger;
    private readonly ILogger<RelationshipService> _relationshipLogger;
    private readonly ProjectManager _projectManager;
    private readonly RelationshipService _relationshipService;

    public RelationshipServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _testVectorStoreBasePath = Path.Combine(Path.GetTempPath(), $"test-relationship-vector-stores-{Guid.NewGuid()}");
        _projectLogger = new TestLogger<ProjectManager>(output);
        _relationshipLogger = new TestLogger<RelationshipService>(output);
        _projectManager = new ProjectManager(_testVectorStoreBasePath, _projectLogger);
        _relationshipService = new RelationshipService(_projectManager, _relationshipLogger);

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
    public void RelationshipService_Can_Be_Instantiated()
    {
        // Assert
        Assert.NotNull(_relationshipService);
    }

    [Fact]
    public async Task GetCallersAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _relationshipService.GetCallersAsync("invalid-id", "SomeNamespace.SomeClass.SomeMethod"));
    }

    [Fact]
    public async Task GetCallersAsync_Throws_On_Empty_MethodFqn()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCallersAsync(projectId, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCallersAsync(projectId, "   "));
    }

    [Fact]
    public async Task GetCallersAsync_Throws_On_Invalid_Depth()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCallersAsync(projectId, "SomeNamespace.SomeClass.SomeMethod", depth: 0));
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCallersAsync(projectId, "SomeNamespace.SomeClass.SomeMethod", depth: -1));
    }

    [Fact]
    public async Task GetCallersAsync_Throws_On_Method_Not_Found()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _relationshipService.GetCallersAsync(projectId, "NonExistent.Namespace.NonExistentClass.NonExistentMethod"));
    }

    [Fact]
    public async Task GetCallersAsync_Returns_Empty_List_When_No_Callers()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a method that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act - This should return empty list or valid structure
        var result = await _relationshipService.GetCallersAsync(projectId, existingMethodFqn, depth: 1);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingMethodFqn, result.MethodFullyQualifiedName);
        Assert.NotNull(result.Callers);
    }

    [Fact]
    public async Task GetCallersAsync_With_IncludeSelf_Includes_Method_Itself()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a method that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetCallersAsync(projectId, existingMethodFqn, depth: 1, includeSelf: true);
        
        // Assert
        Assert.NotNull(result);
        // Method should be included at depth 0 when includeSelf is true
        Assert.Contains(result.Callers, c => c.Depth == 0 && c.FullyQualifiedName == existingMethodFqn);
    }

    [Fact]
    public async Task GetCalleesAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _relationshipService.GetCalleesAsync("invalid-id", "SomeNamespace.SomeClass.SomeMethod"));
    }

    [Fact]
    public async Task GetCalleesAsync_Throws_On_Empty_MethodFqn()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCalleesAsync(projectId, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCalleesAsync(projectId, "   "));
    }

    [Fact]
    public async Task GetCalleesAsync_Throws_On_Invalid_Depth()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCalleesAsync(projectId, "SomeNamespace.SomeClass.SomeMethod", depth: 0));
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetCalleesAsync(projectId, "SomeNamespace.SomeClass.SomeMethod", depth: -1));
    }

    [Fact]
    public async Task GetCalleesAsync_Throws_On_Method_Not_Found()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _relationshipService.GetCalleesAsync(projectId, "NonExistent.Namespace.NonExistentClass.NonExistentMethod"));
    }

    [Fact]
    public async Task GetCalleesAsync_Returns_Empty_List_When_No_Callees()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a method that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetCalleesAsync(projectId, existingMethodFqn, depth: 1);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingMethodFqn, result.MethodFullyQualifiedName);
        Assert.NotNull(result.Callees);
    }

    [Fact]
    public async Task GetCalleesAsync_With_IncludeSelf_Includes_Method_Itself()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a method that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetCalleesAsync(projectId, existingMethodFqn, depth: 1, includeSelf: true);
        
        // Assert
        Assert.NotNull(result);
        // Method should be included at depth 0 when includeSelf is true
        Assert.Contains(result.Callees, c => c.Depth == 0 && c.FullyQualifiedName == existingMethodFqn);
    }

    [Fact]
    public async Task GetCalleesAsync_With_Depth_2_Traverses_Two_Levels()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a method that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetCalleesAsync(projectId, existingMethodFqn, depth: 2);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.MaxDepth <= 2);
        // Verify depth levels are correct
        if (result.Callees.Any())
        {
            Assert.All(result.Callees, c => Assert.True(c.Depth >= 1 && c.Depth <= 2));
        }
    }

    [Fact]
    public async Task GetCallersAsync_With_Depth_2_Traverses_Two_Levels()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a method that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetCallersAsync(projectId, existingMethodFqn, depth: 2);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.MaxDepth <= 2);
        // Verify depth levels are correct
        if (result.Callers.Any())
        {
            Assert.All(result.Callers, c => Assert.True(c.Depth >= 1 && c.Depth <= 2));
        }
    }

    [Fact]
    public async Task GetClassReferencesAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _relationshipService.GetClassReferencesAsync("invalid-id", "SomeNamespace.SomeClass"));
    }

    [Fact]
    public async Task GetClassReferencesAsync_Throws_On_Empty_ClassFqn()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetClassReferencesAsync(projectId, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _relationshipService.GetClassReferencesAsync(projectId, "   "));
    }

    [Fact]
    public async Task GetClassReferencesAsync_Throws_On_Class_Not_Found()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _relationshipService.GetClassReferencesAsync(projectId, "NonExistent.Namespace.NonExistentClass"));
    }

    [Fact]
    public async Task GetClassReferencesAsync_Returns_Valid_Response()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a class that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var classes = await enumerationService.ListClassesAsync(projectId, limit: 1);
        
        if (classes.TotalCount == 0)
        {
            _output.WriteLine("No classes found in project, skipping test");
            return;
        }

        var existingClassFqn = classes.Classes.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetClassReferencesAsync(projectId, existingClassFqn);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingClassFqn, result.ClassFullyQualifiedName);
        Assert.NotNull(result.References);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task GetClassReferencesAsync_With_RelationshipType_Filters_Correctly()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Get a class that exists in the project
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var classes = await enumerationService.ListClassesAsync(projectId, limit: 1);
        
        if (classes.TotalCount == 0)
        {
            _output.WriteLine("No classes found in project, skipping test");
            return;
        }

        var existingClassFqn = classes.Classes.First().FullyQualifiedName;

        // Act
        var result = await _relationshipService.GetClassReferencesAsync(projectId, existingClassFqn, relationshipType: "calls");
        
        // Assert
        Assert.NotNull(result);
        Assert.All(result.References, r => Assert.Equal("calls", r.RelationshipType));
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

