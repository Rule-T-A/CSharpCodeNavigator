using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class AnalysisResultTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var result = new AnalysisResult();

        // Assert
        Assert.NotNull(result.MethodCalls);
        Assert.Empty(result.MethodCalls);
        Assert.NotNull(result.MethodDefinitions);
        Assert.Empty(result.MethodDefinitions);
        Assert.NotNull(result.ClassDefinitions);
        Assert.Empty(result.ClassDefinitions);
        Assert.Equal(0, result.MethodsAnalyzed);
        Assert.Equal(0, result.FilesProcessed);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
        Assert.Equal(0, result.MethodCallCount);
        Assert.Equal(0, result.MethodDefinitionCount);
        Assert.Equal(0, result.ClassDefinitionCount);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var methodCalls = new List<MethodCallInfo>
        {
            new("Caller1", "Callee1", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file1.cs", 1),
            new("Caller2", "Callee2", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file2.cs", 2)
        };
        var errors = new List<string> { "Error1", "Error2" };

        // Act
        var result = new AnalysisResult(methodCalls, 5, 3, errors);

        // Assert
        Assert.Equal(methodCalls, result.MethodCalls);
        Assert.NotNull(result.MethodDefinitions);
        Assert.Empty(result.MethodDefinitions);
        Assert.NotNull(result.ClassDefinitions);
        Assert.Empty(result.ClassDefinitions);
        Assert.Equal(5, result.MethodsAnalyzed);
        Assert.Equal(3, result.FilesProcessed);
        Assert.Equal(errors, result.Errors);
        Assert.Equal(2, result.MethodCallCount);
        Assert.Equal(0, result.MethodDefinitionCount);
        Assert.Equal(0, result.ClassDefinitionCount);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void AddMethodCall_ValidCall_AddsToMethodCalls()
    {
        // Arrange
        var result = new AnalysisResult();
        var methodCall = new MethodCallInfo("Caller", "Callee", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file.cs", 1);

        // Act
        result.AddMethodCall(methodCall);

        // Assert
        Assert.Single(result.MethodCalls);
        Assert.Equal(methodCall, result.MethodCalls[0]);
        Assert.Equal(1, result.MethodCallCount);
    }

    [Fact]
    public void AddMethodDefinition_ValidDefinition_AddsToMethodDefinitions()
    {
        // Arrange
        var result = new AnalysisResult();
        var methodDef = new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "string",
            parameters: new List<string> { "int" },
            accessModifier: "public",
            isStatic: false,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Act
        result.AddMethodDefinition(methodDef);

        // Assert
        Assert.Single(result.MethodDefinitions);
        Assert.Equal(methodDef, result.MethodDefinitions[0]);
        Assert.Equal(1, result.MethodDefinitionCount);
    }

    [Fact]
    public void AddMethodDefinition_NullDefinition_DoesNotAdd()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act
        result.AddMethodDefinition(null!);

        // Assert
        Assert.Empty(result.MethodDefinitions);
        Assert.Equal(0, result.MethodDefinitionCount);
    }

    [Fact]
    public void AddClassDefinition_ValidDefinition_AddsToClassDefinitions()
    {
        // Arrange
        var result = new AnalysisResult();
        var classDef = new ClassDefinitionInfo(
            "TestClass", "TestNamespace", "TestNamespace.TestClass", "public",
            false, false, false, "", new List<string>(), "TestFile.cs", 10, 5, 3, 2);

        // Act
        result.AddClassDefinition(classDef);

        // Assert
        Assert.Single(result.ClassDefinitions);
        Assert.Equal(classDef, result.ClassDefinitions[0]);
        Assert.Equal(1, result.ClassDefinitionCount);
    }

    [Fact]
    public void AddClassDefinition_NullDefinition_DoesNotAdd()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act
        result.AddClassDefinition(null!);

        // Assert
        Assert.Empty(result.ClassDefinitions);
        Assert.Equal(0, result.ClassDefinitionCount);
    }

    [Fact]
    public void AddMethodCall_NullCall_DoesNotAdd()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act
        result.AddMethodCall(null!);

        // Assert
        Assert.Empty(result.MethodCalls);
        Assert.Equal(0, result.MethodCallCount);
    }

    [Fact]
    public void AddError_ValidError_AddsToErrors()
    {
        // Arrange
        var result = new AnalysisResult();
        var error = "Test error message";

        // Act
        result.AddError(error);

        // Assert
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void AddError_EmptyOrNullError_DoesNotAdd()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act
        result.AddError("");
        result.AddError(null!);

        // Assert
        Assert.Empty(result.Errors);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void AddErrors_ValidErrors_AddsAllErrors()
    {
        // Arrange
        var result = new AnalysisResult();
        var errors = new[] { "Error1", "Error2", "Error3" };

        // Act
        result.AddErrors(errors);

        // Assert
        Assert.Equal(3, result.Errors.Count);
        Assert.Equal(errors, result.Errors);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void AddErrors_NullEnumerable_DoesNotAdd()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act
        result.AddErrors(null!);

        // Assert
        Assert.Empty(result.Errors);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void MethodCallCount_ReturnsCorrectCount()
    {
        // Arrange
        var result = new AnalysisResult();
        result.AddMethodCall(new MethodCallInfo("Caller1", "Callee1", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file1.cs", 1));
        result.AddMethodCall(new MethodCallInfo("Caller2", "Callee2", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file2.cs", 2));
        result.AddMethodCall(new MethodCallInfo("Caller3", "Callee3", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file3.cs", 3));

        // Act & Assert
        Assert.Equal(3, result.MethodCallCount);
    }

    [Fact]
    public void IsSuccessful_NoErrors_ReturnsTrue()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act & Assert
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void IsSuccessful_WithErrors_ReturnsFalse()
    {
        // Arrange
        var result = new AnalysisResult();
        result.AddError("Test error");

        // Act & Assert
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var result = new AnalysisResult();
        result.MethodsAnalyzed = 10;
        result.FilesProcessed = 5;
        result.AddMethodCall(new MethodCallInfo("Caller", "Callee", "CallerClass", "CalleeClass", "CallerNs", "CalleeNs", "file.cs", 1));
        result.AddMethodDefinition(new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "string",
            parameters: new List<string>(),
            accessModifier: "public",
            isStatic: false,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        ));
        result.AddClassDefinition(new ClassDefinitionInfo(
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass",
            accessModifier: "public",
            isStatic: false,
            isAbstract: false,
            isSealed: false,
            baseClass: "",
            interfaces: new List<string>(),
            filePath: "TestClass.cs",
            lineNumber: 1,
            methodCount: 5,
            propertyCount: 3,
            fieldCount: 2
        ));
        result.AddError("Test error");

        // Act
        var resultString = result.ToString();

        // Assert
        Assert.Contains("1 method calls found", resultString);
        Assert.Contains("1 method definitions found", resultString);
        Assert.Contains("1 class definitions found", resultString);
        Assert.Contains("10 methods analyzed", resultString);
        Assert.Contains("5 files processed", resultString);
        Assert.Contains("1 errors", resultString);
    }
}
