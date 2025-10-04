using CodeAnalyzer.Roslyn;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Arrange & Act
        var analyzer = new RoslynAnalyzer();

        // Assert
        Assert.NotNull(analyzer);
        Assert.Equal("1.0.0-phase1", analyzer.Version);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_NotImplemented_ThrowsNotImplementedException()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var projectPath = "test.csproj";

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => analyzer.AnalyzeProjectAsync(projectPath));
    }

    [Fact]
    public async Task AnalyzeFileAsync_NotImplemented_ThrowsNotImplementedException()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var filePath = "test.cs";

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => analyzer.AnalyzeFileAsync(filePath));
    }
}
