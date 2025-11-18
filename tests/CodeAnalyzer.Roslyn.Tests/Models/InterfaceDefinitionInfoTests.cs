using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class InterfaceDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var interfaceDef = new InterfaceDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, interfaceDef.InterfaceName);
        Assert.Equal(string.Empty, interfaceDef.Namespace);
        Assert.Equal(string.Empty, interfaceDef.FullyQualifiedName);
        Assert.Equal(string.Empty, interfaceDef.AccessModifier);
        Assert.NotNull(interfaceDef.BaseInterfaces);
        Assert.Empty(interfaceDef.BaseInterfaces);
        Assert.Equal(0, interfaceDef.MethodCount);
        Assert.Equal(0, interfaceDef.PropertyCount);
        Assert.Equal(string.Empty, interfaceDef.FilePath);
        Assert.Equal(0, interfaceDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var baseInterfaces = new List<string> { "IBaseInterface1", "IBaseInterface2" };

        // Act
        var interfaceDef = new InterfaceDefinitionInfo(
            interfaceName: "ITestInterface",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.ITestInterface",
            accessModifier: "public",
            baseInterfaces: baseInterfaces,
            methodCount: 5,
            propertyCount: 3,
            filePath: "ITestInterface.cs",
            lineNumber: 10
        );

        // Assert
        Assert.Equal("ITestInterface", interfaceDef.InterfaceName);
        Assert.Equal("TestNamespace", interfaceDef.Namespace);
        Assert.Equal("TestNamespace.ITestInterface", interfaceDef.FullyQualifiedName);
        Assert.Equal("public", interfaceDef.AccessModifier);
        Assert.Equal(2, interfaceDef.BaseInterfaces.Count);
        Assert.Equal("IBaseInterface1", interfaceDef.BaseInterfaces[0]);
        Assert.Equal("IBaseInterface2", interfaceDef.BaseInterfaces[1]);
        Assert.Equal(5, interfaceDef.MethodCount);
        Assert.Equal(3, interfaceDef.PropertyCount);
        Assert.Equal("ITestInterface.cs", interfaceDef.FilePath);
        Assert.Equal(10, interfaceDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithNullBaseInterfaces_HandlesGracefully()
    {
        // Act
        var interfaceDef = new InterfaceDefinitionInfo(
            interfaceName: "ITestInterface",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.ITestInterface",
            accessModifier: "public",
            baseInterfaces: null!,
            methodCount: 0,
            propertyCount: 0,
            filePath: "ITestInterface.cs",
            lineNumber: 1
        );

        // Assert
        Assert.NotNull(interfaceDef.BaseInterfaces);
        Assert.Empty(interfaceDef.BaseInterfaces);
    }

    [Fact]
    public void ToString_WithBaseInterfaces_ReturnsFormattedString()
    {
        // Arrange
        var baseInterfaces = new List<string> { "IBaseInterface1", "IBaseInterface2" };
        var interfaceDef = new InterfaceDefinitionInfo(
            interfaceName: "ITestInterface",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.ITestInterface",
            accessModifier: "public",
            baseInterfaces: baseInterfaces,
            methodCount: 5,
            propertyCount: 3,
            filePath: "ITestInterface.cs",
            lineNumber: 10
        );

        // Act
        var result = interfaceDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("interface", result);
        Assert.Contains("ITestInterface", result);
        Assert.Contains("IBaseInterface1", result);
        Assert.Contains("IBaseInterface2", result);
        Assert.Contains("line 10", result);
        Assert.Contains("ITestInterface.cs", result);
    }

    [Fact]
    public void ToString_WithNoBaseInterfaces_ReturnsFormattedString()
    {
        // Arrange
        var interfaceDef = new InterfaceDefinitionInfo(
            interfaceName: "ISimpleInterface",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.ISimpleInterface",
            accessModifier: "internal",
            baseInterfaces: new List<string>(),
            methodCount: 2,
            propertyCount: 1,
            filePath: "ISimpleInterface.cs",
            lineNumber: 5
        );

        // Act
        var result = interfaceDef.ToString();

        // Assert
        Assert.Contains("internal", result);
        Assert.Contains("ISimpleInterface", result);
        Assert.Contains("line 5", result);
        Assert.DoesNotContain(":", result); // No inheritance syntax
    }
}

