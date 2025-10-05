using CodeAnalyzer.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

    [Fact]
    public async Task CreateCompilationFromFilesAsync_ParsesSyntaxAndCreatesSemanticModel()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var testFile = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "TestData", "SimpleClass.cs");
        testFile = Path.GetFullPath(testFile);

        Assert.True(File.Exists(testFile), $"Test file not found: {testFile}");

        // Act
        var compilation = await analyzer.CreateCompilationFromFilesAsync(testFile);

        // Assert: compilation created
        Assert.NotNull(compilation);
        Assert.IsType<CSharpCompilation>(compilation);

        // Assert: has at least one syntax tree
        Assert.NotEmpty(compilation.SyntaxTrees);

        // Assert: semantic model can be created and there are no diagnostics errors
        var tree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(tree);
        Assert.NotNull(semanticModel);

        var diagnostics = compilation.GetDiagnostics();
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task ExtractMethodDeclarations_FindsMethodsAndBuildsFqn()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var testFile = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "TestData", "MultiNamespace.cs");
        testFile = Path.GetFullPath(testFile);
        Assert.True(File.Exists(testFile), $"Test file not found: {testFile}");

        var compilation = await analyzer.CreateCompilationFromFilesAsync(testFile);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);

        // Act
        var methods = analyzer.ExtractMethodDeclarations(tree, model);

        // Assert
        Assert.NotNull(methods);
        Assert.Contains("First.Namespace.Alpha.DoSomething", methods);
        Assert.Contains("First.Namespace.Alpha.DoStatic", methods);
        Assert.Contains("Second.Namespace.Inner.Beta.Add", methods);
        Assert.Contains("Second.Namespace.Inner.Beta.Name", methods);
    }
}
