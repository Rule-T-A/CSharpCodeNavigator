using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class EnumDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var enumDef = new EnumDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, enumDef.EnumName);
        Assert.Equal(string.Empty, enumDef.Namespace);
        Assert.Equal(string.Empty, enumDef.FullyQualifiedName);
        Assert.Equal(string.Empty, enumDef.AccessModifier);
        Assert.Equal("int", enumDef.UnderlyingType);
        Assert.NotNull(enumDef.Values);
        Assert.Empty(enumDef.Values);
        Assert.Equal(string.Empty, enumDef.FilePath);
        Assert.Equal(0, enumDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var values = new List<EnumValueInfo>
        {
            new EnumValueInfo("Value1", 0, 5),
            new EnumValueInfo("Value2", 1, 6)
        };

        // Act
        var enumDef = new EnumDefinitionInfo(
            enumName: "TestEnum",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestEnum",
            accessModifier: "public",
            underlyingType: "int",
            values: values,
            filePath: "TestEnum.cs",
            lineNumber: 3
        );

        // Assert
        Assert.Equal("TestEnum", enumDef.EnumName);
        Assert.Equal("TestNamespace", enumDef.Namespace);
        Assert.Equal("TestNamespace.TestEnum", enumDef.FullyQualifiedName);
        Assert.Equal("public", enumDef.AccessModifier);
        Assert.Equal("int", enumDef.UnderlyingType);
        Assert.Equal(2, enumDef.Values.Count);
        Assert.Equal("TestEnum.cs", enumDef.FilePath);
        Assert.Equal(3, enumDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithNullValues_HandlesGracefully()
    {
        // Act
        var enumDef = new EnumDefinitionInfo(
            enumName: "TestEnum",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestEnum",
            accessModifier: "public",
            underlyingType: "byte",
            values: null!,
            filePath: "TestEnum.cs",
            lineNumber: 1
        );

        // Assert
        Assert.NotNull(enumDef.Values);
        Assert.Empty(enumDef.Values);
    }

    [Fact]
    public void ToString_WithValues_ReturnsFormattedString()
    {
        // Arrange
        var values = new List<EnumValueInfo>
        {
            new EnumValueInfo("Value1", 0, 5),
            new EnumValueInfo("Value2", 1, 6)
        };
        var enumDef = new EnumDefinitionInfo(
            enumName: "TestEnum",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestEnum",
            accessModifier: "public",
            underlyingType: "int",
            values: values,
            filePath: "TestEnum.cs",
            lineNumber: 3
        );

        // Act
        var result = enumDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("enum", result);
        Assert.Contains("TestEnum", result);
        Assert.Contains("int", result);
        Assert.Contains("Value1 = 0", result);
        Assert.Contains("Value2 = 1", result);
        Assert.Contains("line 3", result);
        Assert.Contains("TestEnum.cs", result);
    }

    [Fact]
    public void ToString_WithNoValues_ReturnsFormattedString()
    {
        // Arrange
        var enumDef = new EnumDefinitionInfo(
            enumName: "EmptyEnum",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.EmptyEnum",
            accessModifier: "private",
            underlyingType: "byte",
            values: new List<EnumValueInfo>(),
            filePath: "EmptyEnum.cs",
            lineNumber: 1
        );

        // Act
        var result = enumDef.ToString();

        // Assert
        Assert.Contains("private", result);
        Assert.Contains("EmptyEnum", result);
        Assert.Contains("byte", result);
        Assert.Contains("line 1", result);
    }
}

