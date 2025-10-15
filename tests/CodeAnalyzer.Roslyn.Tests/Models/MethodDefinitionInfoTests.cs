using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class MethodDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var methodDef = new MethodDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, methodDef.MethodName);
        Assert.Equal(string.Empty, methodDef.ClassName);
        Assert.Equal(string.Empty, methodDef.Namespace);
        Assert.Equal(string.Empty, methodDef.FullyQualifiedName);
        Assert.Equal(string.Empty, methodDef.ReturnType);
        Assert.NotNull(methodDef.Parameters);
        Assert.Empty(methodDef.Parameters);
        Assert.Equal(string.Empty, methodDef.AccessModifier);
        Assert.False(methodDef.IsStatic);
        Assert.False(methodDef.IsVirtual);
        Assert.False(methodDef.IsAbstract);
        Assert.False(methodDef.IsOverride);
        Assert.Equal(string.Empty, methodDef.FilePath);
        Assert.Equal(0, methodDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var parameters = new List<string> { "string", "int", "bool" };

        // Act
        var methodDef = new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "string",
            parameters: parameters,
            accessModifier: "public",
            isStatic: true,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Assert
        Assert.Equal("TestMethod", methodDef.MethodName);
        Assert.Equal("TestClass", methodDef.ClassName);
        Assert.Equal("TestNamespace", methodDef.Namespace);
        Assert.Equal("TestNamespace.TestClass.TestMethod", methodDef.FullyQualifiedName);
        Assert.Equal("string", methodDef.ReturnType);
        Assert.Equal(parameters, methodDef.Parameters);
        Assert.Equal("public", methodDef.AccessModifier);
        Assert.True(methodDef.IsStatic);
        Assert.False(methodDef.IsVirtual);
        Assert.False(methodDef.IsAbstract);
        Assert.False(methodDef.IsOverride);
        Assert.Equal("TestClass.cs", methodDef.FilePath);
        Assert.Equal(42, methodDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithNullParameters_HandlesGracefully()
    {
        // Arrange & Act
        var methodDef = new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "string",
            parameters: null!,
            accessModifier: "public",
            isStatic: false,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Assert
        Assert.NotNull(methodDef.Parameters);
        Assert.Empty(methodDef.Parameters);
    }

    [Fact]
    public void ToString_WithAllProperties_ReturnsFormattedString()
    {
        // Arrange
        var parameters = new List<string> { "string", "int" };
        var methodDef = new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "string",
            parameters: parameters,
            accessModifier: "public",
            isStatic: true,
            isVirtual: true,
            isAbstract: false,
            isOverride: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Act
        var result = methodDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("static", result);
        Assert.Contains("virtual", result);
        Assert.Contains("string", result);
        Assert.Contains("TestMethod", result);
        Assert.Contains("string, int", result);
        Assert.Contains("line 42", result);
        Assert.Contains("TestClass.cs", result);
    }

    [Fact]
    public void ToString_WithMinimalProperties_ReturnsFormattedString()
    {
        // Arrange
        var methodDef = new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "void",
            parameters: new List<string>(),
            accessModifier: "private",
            isStatic: false,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            filePath: "TestClass.cs",
            lineNumber: 1
        );

        // Act
        var result = methodDef.ToString();

        // Assert
        Assert.Contains("private", result);
        Assert.Contains("void", result);
        Assert.Contains("TestMethod", result);
        Assert.Contains("()", result);
        Assert.Contains("line 1", result);
    }

    [Fact]
    public void ToString_WithAbstractOverride_ReturnsFormattedString()
    {
        // Arrange
        var methodDef = new MethodDefinitionInfo(
            methodName: "TestMethod",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestMethod",
            returnType: "string",
            parameters: new List<string>(),
            accessModifier: "protected",
            isStatic: false,
            isVirtual: false,
            isAbstract: true,
            isOverride: true,
            filePath: "TestClass.cs",
            lineNumber: 10
        );

        // Act
        var result = methodDef.ToString();

        // Assert
        Assert.Contains("protected", result);
        Assert.Contains("abstract", result);
        Assert.Contains("override", result);
        Assert.Contains("string", result);
        Assert.Contains("TestMethod", result);
    }
}
