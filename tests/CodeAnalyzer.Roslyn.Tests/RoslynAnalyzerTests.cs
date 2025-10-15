using CodeAnalyzer.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeAnalyzer.Roslyn.Models;

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
        // Diagnostics may be present depending on environment; should not throw
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public async Task AnalyzeFileAsync_PersistsCalls_WhenVectorStoreProvided()
    {
        // Arrange
        var file = Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "TestData", "CallsSample.cs");
        var writer = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(writer);

        // Act
        var result = await analyzer.AnalyzeFileAsync(Path.GetFullPath(file));

        // Assert
        Assert.NotEmpty(result.MethodCalls);
        Assert.NotEmpty(result.MethodDefinitions);
        Assert.True(writer.Writes.Count >= result.MethodCalls.Count + result.MethodDefinitions.Count);
        
        // Verify we have both method calls and method definitions
        var methodCalls = writer.Writes.Where(w => w.metadata["type"].ToString() == "method_call").ToList();
        var methodDefinitions = writer.Writes.Where(w => w.metadata["type"].ToString() == "method_definition").ToList();
        
        Assert.NotEmpty(methodCalls);
        Assert.NotEmpty(methodDefinitions);
        Assert.Equal(result.MethodCalls.Count, methodCalls.Count);
        Assert.Equal(result.MethodDefinitions.Count, methodDefinitions.Count);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_CrossFile_InterfaceAndExtensionCallsResolved()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "CrossFile");
        dir = Path.GetFullPath(dir);
        var files = new[]
        {
            Path.Combine(dir, "IService.cs"),
            Path.Combine(dir, "ServiceImpl.cs"),
            Path.Combine(dir, "Extensions.cs"),
            Path.Combine(dir, "Consumer.cs")
        };
        foreach (var f in files) Assert.True(File.Exists(f), $"Missing: {f}");

        // Build a temporary ad-hoc compilation from files (simulate project)
        var compilation = await analyzer.CreateCompilationFromFilesAsync(files);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        Assert.NotNull(model);

        // Extract calls across trees
        var allCalls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
        {
            var m = compilation.GetSemanticModel(t);
            allCalls.AddRange(analyzer.ExtractMethodCalls(t, m));
        }

        // Assert that Consumer.Run calls IService.Work (interface dispatch)
        Assert.Contains(allCalls, c => c.Caller.EndsWith("CrossFile.Use.Consumer.Run") && c.Callee.EndsWith("CrossFile.Svc.IService.Work"));

        // Assert extension call resolved to extension method definition
        Assert.Contains(allCalls, c => c.Caller.EndsWith("CrossFile.Use.Consumer.Run") && c.Callee.EndsWith("CrossFile.Svc.ServiceExtensions.Extra"));
    }

    [Fact]
    public async Task CreateCompilationFromFilesAsync_UnresolvedSymbol_DoesNotCrashAndSkipsCall()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Errors", "Unresolved.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        // Act
        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var calls = analyzer.ExtractMethodCalls(tree, model);

        // Assert: no exceptions, calls either empty or without MissingType.DoThing
        Assert.DoesNotContain(calls, c => c.Callee.Contains("MissingType"));
    }

    [Fact]
    public async Task VirtualDispatch_CallRecordedOnBaseMethodSymbol()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Virtual", "VirtualDispatch.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Collect calls
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Expect: Uses.Run -> Base.Do (recorded against base symbol per Step 1.5 policy)
        Assert.Contains(calls, c => c.Caller.EndsWith("Virtual.Dispatch.Uses.Run") && c.Callee.EndsWith("Virtual.Dispatch.Base.Do"));
    }

    [Fact]
    public async Task AnalyzeFile_WithSyntaxErrors_CollectsDiagnostics_NoThrow()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer().WithOptions(new AnalyzerOptions { IncludeWarningsInErrors = true });
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Errors", "SyntaxError.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        // Act
        var result = await analyzer.AnalyzeFileAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Errors.Count >= 1);
    }

    [Fact]
    public async Task CallsInsideLocalAndLambda_AreAttributedToContainingMethod()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Locals", "LocalAndLambda.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Act
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Assert
        Assert.Contains(calls, c => c.Caller.EndsWith("Local.Lambda.Uses.Run") && c.Callee.Contains("System.Console.WriteLine"));
    }

    [Fact]
    public async Task AttributeConstructorCalls_WhenEnabled_AreRecorded()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer().WithOptions(new AnalyzerOptions { AttributeInitializerCalls = true });
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Attributes", "AttributeCalls.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Act
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Assert: Should find attribute constructor calls
        Assert.Contains(calls, c => c.Callee.Contains("System.SerializableAttribute") && c.Callee.Contains(".ctor"));
        Assert.Contains(calls, c => c.Callee.Contains("System.ObsoleteAttribute") && c.Callee.Contains(".ctor"));
        Assert.Contains(calls, c => c.Callee.Contains("Test.Attributes.TestMethodAttribute") && c.Callee.Contains(".ctor"));
        Assert.Contains(calls, c => c.Callee.Contains("Test.Attributes.TestMethodWithParamsAttribute") && c.Callee.Contains(".ctor"));
    }

    [Fact]
    public async Task AttributeConstructorCalls_WhenDisabled_AreSkipped()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer().WithOptions(new AnalyzerOptions { AttributeInitializerCalls = false });
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Attributes", "AttributeCalls.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Act
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Assert: Should NOT find attribute constructor calls
        Assert.DoesNotContain(calls, c => c.Callee.Contains("System.SerializableAttribute") && c.Callee.Contains(".ctor"));
        Assert.DoesNotContain(calls, c => c.Callee.Contains("System.ObsoleteAttribute") && c.Callee.Contains(".ctor"));
        Assert.DoesNotContain(calls, c => c.Callee.Contains("Test.Attributes.TestMethodAttribute") && c.Callee.Contains(".ctor"));
    }

    [Fact]
    public async Task FieldPropertyInitializerCalls_WhenEnabled_AreRecorded()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer().WithOptions(new AnalyzerOptions { AttributeInitializerCalls = true });
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Initializers", "FieldInitializers.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Act
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Assert: Should find initializer method calls
        Assert.Contains(calls, c => c.Callee.EndsWith("Test.Initializers.InitializerTest.GetDefaultName") && c.Caller.EndsWith("Test.Initializers.InitializerTest"));
        Assert.Contains(calls, c => c.Callee.EndsWith("Test.Initializers.InitializerTest.CalculateCount") && c.Caller.EndsWith("Test.Initializers.InitializerTest"));
        Assert.Contains(calls, c => c.Callee.EndsWith("Test.Initializers.InitializerTest.CreateDescription") && c.Caller.EndsWith("Test.Initializers.InitializerTest"));
    }

    [Fact]
    public async Task FieldPropertyInitializerCalls_WhenDisabled_AreSkipped()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer().WithOptions(new AnalyzerOptions { AttributeInitializerCalls = false });
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Initializers", "FieldInitializers.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Act
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Assert: Should NOT find initializer method calls
        Assert.DoesNotContain(calls, c => c.Callee.EndsWith("Test.Initializers.InitializerTest.GetDefaultName"));
        Assert.DoesNotContain(calls, c => c.Callee.EndsWith("Test.Initializers.InitializerTest.CalculateCount"));
        Assert.DoesNotContain(calls, c => c.Callee.EndsWith("Test.Initializers.InitializerTest.CreateDescription"));
    }

    [Fact]
    public async Task LocalLambdaBehavior_RemainsConsistent_WithAttributeInitializerCallsEnabled()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer().WithOptions(new AnalyzerOptions { AttributeInitializerCalls = true });
        var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Locals", "LocalAndLambda.cs");
        file = Path.GetFullPath(file);
        Assert.True(File.Exists(file));

        var compilation = await analyzer.CreateCompilationFromFilesAsync(file);

        // Act
        var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
        foreach (var t in compilation.SyntaxTrees)
            calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

        // Assert: Local/lambda behavior should remain unchanged
        Assert.Contains(calls, c => c.Caller.EndsWith("Local.Lambda.Uses.Run") && c.Callee.Contains("System.Console.WriteLine"));
    }
}
