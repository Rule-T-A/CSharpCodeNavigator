using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Api.Services;
using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CodeAnalyzer.Api.Tests.Services;

/// <summary>
/// Tests for CodeElementService.
/// </summary>
public class CodeElementServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testVectorStoreBasePath;
    private readonly ILogger<ProjectManager> _projectLogger;
    private readonly ILogger<CodeElementService> _codeElementLogger;
    private readonly ProjectManager _projectManager;
    private readonly CodeElementService _codeElementService;

    public CodeElementServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _testVectorStoreBasePath = Path.Combine(Path.GetTempPath(), $"test-code-element-vector-stores-{Guid.NewGuid()}");
        _projectLogger = new TestLogger<ProjectManager>(output);
        _codeElementLogger = new TestLogger<CodeElementService>(output);
        _projectManager = new ProjectManager(_testVectorStoreBasePath, _projectLogger);
        _codeElementService = new CodeElementService(_projectManager, _codeElementLogger);

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
    public void CodeElementService_Can_Be_Instantiated()
    {
        // Assert
        Assert.NotNull(_codeElementService);
    }

    [Fact]
    public async Task GetMethodAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _codeElementService.GetMethodAsync("invalid-id", "SomeNamespace.SomeClass.SomeMethod"));
    }

    [Fact]
    public async Task GetMethodAsync_Throws_On_Empty_MethodFqn()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _codeElementService.GetMethodAsync(projectId, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _codeElementService.GetMethodAsync(projectId, "   "));
    }

    [Fact]
    public async Task GetMethodAsync_Throws_On_Method_Not_Found()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _codeElementService.GetMethodAsync(projectId, "NonExistent.Namespace.NonExistentClass.NonExistentMethod"));
    }

    [Fact]
    public async Task GetMethodAsync_Returns_Method_Details_For_Existing_Method()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // First, get a list of methods to find an existing one
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _codeElementService.GetMethodAsync(projectId, existingMethodFqn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingMethodFqn, result.FullyQualifiedName);
        Assert.NotEmpty(result.MethodName);
        Assert.NotEmpty(result.ClassName);
        Assert.NotEmpty(result.FilePath);
        Assert.True(result.LineNumber > 0);
    }

    [Fact]
    public async Task GetClassAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _codeElementService.GetClassAsync("invalid-id", "SomeNamespace.SomeClass"));
    }

    [Fact]
    public async Task GetClassAsync_Throws_On_Empty_ClassFqn()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _codeElementService.GetClassAsync(projectId, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _codeElementService.GetClassAsync(projectId, "   "));
    }

    [Fact]
    public async Task GetClassAsync_Throws_On_Class_Not_Found()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _codeElementService.GetClassAsync(projectId, "NonExistent.Namespace.NonExistentClass"));
    }

    [Fact]
    public async Task GetClassAsync_Returns_Class_Details_For_Existing_Class()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // First, get a list of classes to find an existing one
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var classes = await enumerationService.ListClassesAsync(projectId, limit: 1);
        
        if (classes.TotalCount == 0)
        {
            _output.WriteLine("No classes found in project, skipping test");
            return;
        }

        var existingClassFqn = classes.Classes.First().FullyQualifiedName;

        // Act
        var result = await _codeElementService.GetClassAsync(projectId, existingClassFqn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingClassFqn, result.FullyQualifiedName);
        Assert.NotEmpty(result.ClassName);
        Assert.NotEmpty(result.FilePath);
        Assert.True(result.LineNumber > 0);
        Assert.True(result.MethodCount >= 0);
        Assert.True(result.PropertyCount >= 0);
        Assert.True(result.FieldCount >= 0);
    }

    [Fact]
    public async Task GetClassMethodsAsync_Throws_On_Invalid_ProjectId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _codeElementService.GetClassMethodsAsync("invalid-id", "SomeNamespace.SomeClass"));
    }

    [Fact]
    public async Task GetClassMethodsAsync_Throws_On_Empty_ClassFqn()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _codeElementService.GetClassMethodsAsync(projectId, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _codeElementService.GetClassMethodsAsync(projectId, "   "));
    }

    [Fact]
    public async Task GetClassMethodsAsync_Throws_On_Class_Not_Found()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _codeElementService.GetClassMethodsAsync(projectId, "NonExistent.Namespace.NonExistentClass"));
    }

    [Fact]
    public async Task GetClassMethodsAsync_Returns_Methods_For_Existing_Class()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // First, get a list of classes to find an existing one
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var classes = await enumerationService.ListClassesAsync(projectId, limit: 1);
        
        if (classes.TotalCount == 0)
        {
            _output.WriteLine("No classes found in project, skipping test");
            return;
        }

        var existingClassFqn = classes.Classes.First().FullyQualifiedName;

        // Act
        var result = await _codeElementService.GetClassMethodsAsync(projectId, existingClassFqn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingClassFqn, result.ClassFullyQualifiedName);
        Assert.NotNull(result.Methods);
        Assert.Equal(result.Methods.Count, result.TotalCount);
        
        // Verify all methods belong to the requested class
        foreach (var method in result.Methods)
        {
            Assert.NotEmpty(method.FullyQualifiedName);
            Assert.NotEmpty(method.MethodName);
        }
    }

    [Fact]
    public async Task GetMethodAsync_Returns_Complete_Method_Information()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // First, get a list of methods to find an existing one
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var methods = await enumerationService.ListMethodsAsync(projectId, limit: 1);
        
        if (methods.TotalCount == 0)
        {
            _output.WriteLine("No methods found in project, skipping test");
            return;
        }

        var existingMethodFqn = methods.Methods.First().FullyQualifiedName;

        // Act
        var result = await _codeElementService.GetMethodAsync(projectId, existingMethodFqn);

        // Assert - verify all properties are populated
        Assert.NotEmpty(result.FullyQualifiedName);
        Assert.NotEmpty(result.MethodName);
        Assert.NotEmpty(result.ClassName);
        Assert.NotEmpty(result.Namespace);
        Assert.NotEmpty(result.ReturnType);
        Assert.NotEmpty(result.AccessModifier);
        Assert.NotNull(result.Parameters);
        Assert.NotEmpty(result.FilePath);
        Assert.True(result.LineNumber > 0);
    }

    [Fact]
    public async Task GetClassAsync_Returns_Complete_Class_Information()
    {
        // Arrange
        var solutionDir = FindSolutionDirectory();
        var projectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectId = await _projectManager.IndexProjectAsync(projectPath, "RoslynProject");
        
        // Wait for indexing to complete
        await WaitForIndexingComplete(projectId);

        // First, get a list of classes to find an existing one
        var enumerationService = new EnumerationService(_projectManager, new TestLogger<EnumerationService>(_output));
        var classes = await enumerationService.ListClassesAsync(projectId, limit: 1);
        
        if (classes.TotalCount == 0)
        {
            _output.WriteLine("No classes found in project, skipping test");
            return;
        }

        var existingClassFqn = classes.Classes.First().FullyQualifiedName;

        // Act
        var result = await _codeElementService.GetClassAsync(projectId, existingClassFqn);

        // Assert - verify all properties are populated
        Assert.NotEmpty(result.FullyQualifiedName);
        Assert.NotEmpty(result.ClassName);
        Assert.NotEmpty(result.Namespace);
        Assert.NotEmpty(result.AccessModifier);
        Assert.NotNull(result.Interfaces);
        Assert.NotEmpty(result.FilePath);
        Assert.True(result.LineNumber > 0);
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

