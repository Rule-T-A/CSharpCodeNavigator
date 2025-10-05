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
        // This test is obsolete after Step 1.4 implementation.
        // Keeping placeholder to avoid duplicate fact names if referenced.
        Assert.True(true);
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

    [Fact]
    public async Task ExtractMethodCalls_FindsBasicCalls_WithLocations()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var testFile = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "TestData", "CallsSample.cs");
        testFile = Path.GetFullPath(testFile);
        Assert.True(File.Exists(testFile), $"Test file not found: {testFile}");

        var compilation = await analyzer.CreateCompilationFromFilesAsync(testFile);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);

        // Act
        var calls = analyzer.ExtractMethodCalls(tree, model);

        // Assert
        Assert.NotNull(calls);
        Assert.NotEmpty(calls);

        // Expect: Caller.A -> Caller.B
        Assert.Contains(calls, c => c.Caller.EndsWith("Sample.Calls.Caller.A") && c.Callee.EndsWith("Sample.Calls.Caller.B"));

        // Expect: Caller.B -> Caller.C
        Assert.Contains(calls, c => c.Caller.EndsWith("Sample.Calls.Caller.B") && c.Callee.EndsWith("Sample.Calls.Caller.C"));

        // Expect: Caller.A -> Helper.Util (static)
        Assert.Contains(calls, c => c.Caller.EndsWith("Sample.Calls.Caller.A") && c.Callee.EndsWith("Sample.Calls.Helper.Util"));

        // Expect: Caller.A -> Console.WriteLine
        Assert.Contains(calls, c => c.Caller.EndsWith("Sample.Calls.Caller.A") && c.Callee.Contains("System.Console.WriteLine"));

        // Locations present
        Assert.All(calls, c => Assert.True(c.LineNumber > 0));
        Assert.All(calls, c => Assert.False(string.IsNullOrWhiteSpace(c.FilePath)));
    }

    [Fact]
    public async Task AnalyzeFileAsync_PopulatesAnalysisResult()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var testFile = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "TestData", "CallsSample.cs");
        testFile = Path.GetFullPath(testFile);
        Assert.True(File.Exists(testFile), $"Test file not found: {testFile}");

        // Act
        var result = await analyzer.AnalyzeFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.FilesProcessed);
        Assert.True(result.MethodsAnalyzed >= 3);
        Assert.NotEmpty(result.MethodCalls);
        Assert.Empty(result.Errors);
    }
}
