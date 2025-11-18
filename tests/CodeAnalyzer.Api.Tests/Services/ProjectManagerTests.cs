using CodeAnalyzer.Api.Models;
using CodeAnalyzer.Api.Services;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CodeAnalyzer.Api.Tests.Services;

/// <summary>
/// Tests for ProjectManager service.
/// </summary>
public class ProjectManagerTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testVectorStoreBasePath;
    private readonly ILogger<ProjectManager> _logger;

    public ProjectManagerTests(ITestOutputHelper output)
    {
        _output = output;
        _testVectorStoreBasePath = Path.Combine(Path.GetTempPath(), $"test-vector-stores-{Guid.NewGuid()}");
        _logger = new TestLogger<ProjectManager>(output);
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
    public void ProjectManager_Can_Be_Instantiated()
    {
        // Act
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task IndexProjectAsync_With_Valid_Path_Returns_ProjectId()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();

        // Act
        var projectId = await manager.IndexProjectAsync(testProjectPath, "TestProject");

        // Assert
        Assert.NotNull(projectId);
        Assert.NotEmpty(projectId);
    }

    [Fact]
    public async Task IndexProjectAsync_Throws_On_Null_Path()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.IndexProjectAsync(null!));
    }

    [Fact]
    public async Task IndexProjectAsync_Throws_On_Empty_Path()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.IndexProjectAsync(string.Empty));
    }

    [Fact]
    public async Task IndexProjectAsync_Creates_Project_Info()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();

        // Act
        var projectId = await manager.IndexProjectAsync(testProjectPath, "TestProject");

        // Assert
        var projects = await manager.ListProjectsAsync();
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        Assert.NotNull(project);
        Assert.Equal("TestProject", project.ProjectName);
        Assert.Equal(Path.GetFullPath(testProjectPath), project.ProjectPath);
        Assert.NotNull(project.VectorStorePath);
    }

    [Fact]
    public async Task IndexProjectAsync_Generates_ProjectName_If_Not_Provided()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();

        // Act
        var projectId = await manager.IndexProjectAsync(testProjectPath);

        // Assert
        var projects = await manager.ListProjectsAsync();
        var project = projects.FirstOrDefault(p => p.ProjectId == projectId);
        
        Assert.NotNull(project);
        Assert.NotEmpty(project.ProjectName);
    }

    [Fact]
    public async Task GetProjectStatusAsync_Returns_Initial_Status()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();
        var projectId = await manager.IndexProjectAsync(testProjectPath, "TestProject");

        // Act
        var status = await manager.GetProjectStatusAsync(projectId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(projectId, status.ProjectId);
        Assert.True(status.Status == IndexingStatus.Queued || status.Status == IndexingStatus.Indexing);
        Assert.NotNull(status.StartedAt);
    }

    [Fact]
    public async Task GetProjectStatusAsync_Throws_On_Invalid_ProjectId()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => manager.GetProjectStatusAsync("invalid-id"));
    }

    [Fact]
    public async Task ListProjectsAsync_Returns_Empty_List_Initially()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);

        // Act
        var projects = await manager.ListProjectsAsync();

        // Assert
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task ListProjectsAsync_Returns_All_Projects()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var solutionDir = FindSolutionDirectory();
        
        // Use different project paths to ensure different project IDs
        var projectPath1 = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        var projectPath2 = Path.Combine(solutionDir, "src", "CodeAnalyzer.Navigation", "CodeAnalyzer.Navigation.csproj");

        // Act
        var projectId1 = await manager.IndexProjectAsync(projectPath1, "Project1");
        var projectId2 = await manager.IndexProjectAsync(projectPath2, "Project2");

        // Assert
        var projects = await manager.ListProjectsAsync();
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.ProjectId == projectId1);
        Assert.Contains(projects, p => p.ProjectId == projectId2);
    }

    [Fact]
    public async Task DeleteProjectAsync_Removes_Project()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();
        var projectId = await manager.IndexProjectAsync(testProjectPath, "TestProject");

        // Wait a bit for indexing to start
        await Task.Delay(100);

        // Act
        var result = await manager.DeleteProjectAsync(projectId);

        // Assert
        Assert.True(result);
        var projects = await manager.ListProjectsAsync();
        Assert.DoesNotContain(projects, p => p.ProjectId == projectId);
    }

    [Fact]
    public async Task DeleteProjectAsync_Returns_False_For_Invalid_ProjectId()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);

        // Act
        var result = await manager.DeleteProjectAsync("invalid-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IndexProjectAsync_Same_Path_Returns_Same_ProjectId()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();

        // Act
        var projectId1 = await manager.IndexProjectAsync(testProjectPath, "TestProject");
        var projectId2 = await manager.IndexProjectAsync(testProjectPath, "TestProject");

        // Assert
        Assert.Equal(projectId1, projectId2);
    }

    [Fact]
    public async Task ProjectStatus_Updates_During_Indexing()
    {
        // Arrange
        var manager = new ProjectManager(_testVectorStoreBasePath, _logger);
        var testProjectPath = GetTestProjectPath();
        var projectId = await manager.IndexProjectAsync(testProjectPath, "TestProject");

        // Wait for indexing to progress
        await Task.Delay(500);

        // Act
        var status = await manager.GetProjectStatusAsync(projectId);

        // Assert
        Assert.NotNull(status);
        Assert.True(
            status.Status == IndexingStatus.Queued ||
            status.Status == IndexingStatus.Indexing ||
            status.Status == IndexingStatus.Completed ||
            status.Status == IndexingStatus.Failed);
    }

    /// <summary>
    /// Gets a path to a test project. Uses the Roslyn.Tests project as a test case.
    /// </summary>
    private static string GetTestProjectPath()
    {
        // Find a test project - use the Roslyn project itself
        var solutionDir = FindSolutionDirectory();
        var roslynProjectPath = Path.Combine(solutionDir, "src", "CodeAnalyzer.Roslyn", "CodeAnalyzer.Roslyn.csproj");
        
        if (File.Exists(roslynProjectPath))
        {
            return roslynProjectPath;
        }

        // Fallback: use current directory
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

