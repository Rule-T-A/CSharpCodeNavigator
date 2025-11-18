using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class StructDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var structDef = new StructDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, structDef.StructName);
        Assert.Equal(string.Empty, structDef.Namespace);
        Assert.Equal(string.Empty, structDef.FullyQualifiedName);
        Assert.Equal(string.Empty, structDef.AccessModifier);
        Assert.False(structDef.IsReadOnly);
        Assert.False(structDef.IsRef);
        Assert.NotNull(structDef.Interfaces);
        Assert.Empty(structDef.Interfaces);
        Assert.Equal(0, structDef.MethodCount);
        Assert.Equal(0, structDef.PropertyCount);
        Assert.Equal(0, structDef.FieldCount);
        Assert.Equal(string.Empty, structDef.FilePath);
        Assert.Equal(0, structDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var interfaces = new List<string> { "IDisposable", "IComparable" };

        // Act
        var structDef = new StructDefinitionInfo(
            structName: "TestStruct",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestStruct",
            accessModifier: "public",
            isReadOnly: true,
            isRef: false,
            interfaces: interfaces,
            methodCount: 3,
            propertyCount: 2,
            fieldCount: 5,
            filePath: "TestStruct.cs",
            lineNumber: 15
        );

        // Assert
        Assert.Equal("TestStruct", structDef.StructName);
        Assert.Equal("TestNamespace", structDef.Namespace);
        Assert.Equal("TestNamespace.TestStruct", structDef.FullyQualifiedName);
        Assert.Equal("public", structDef.AccessModifier);
        Assert.True(structDef.IsReadOnly);
        Assert.False(structDef.IsRef);
        Assert.Equal(2, structDef.Interfaces.Count);
        Assert.Equal("IDisposable", structDef.Interfaces[0]);
        Assert.Equal("IComparable", structDef.Interfaces[1]);
        Assert.Equal(3, structDef.MethodCount);
        Assert.Equal(2, structDef.PropertyCount);
        Assert.Equal(5, structDef.FieldCount);
        Assert.Equal("TestStruct.cs", structDef.FilePath);
        Assert.Equal(15, structDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithRefStruct_SetsIsRef()
    {
        // Act
        var structDef = new StructDefinitionInfo(
            structName: "RefStruct",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.RefStruct",
            accessModifier: "public",
            isReadOnly: false,
            isRef: true,
            interfaces: new List<string>(),
            methodCount: 0,
            propertyCount: 0,
            fieldCount: 0,
            filePath: "RefStruct.cs",
            lineNumber: 1
        );

        // Assert
        Assert.True(structDef.IsRef);
        Assert.False(structDef.IsReadOnly);
    }

    [Fact]
    public void Constructor_WithNullInterfaces_HandlesGracefully()
    {
        // Act
        var structDef = new StructDefinitionInfo(
            structName: "TestStruct",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestStruct",
            accessModifier: "public",
            isReadOnly: false,
            isRef: false,
            interfaces: null!,
            methodCount: 0,
            propertyCount: 0,
            fieldCount: 0,
            filePath: "TestStruct.cs",
            lineNumber: 1
        );

        // Assert
        Assert.NotNull(structDef.Interfaces);
        Assert.Empty(structDef.Interfaces);
    }

    [Fact]
    public void ToString_WithModifiersAndInterfaces_ReturnsFormattedString()
    {
        // Arrange
        var interfaces = new List<string> { "IDisposable" };
        var structDef = new StructDefinitionInfo(
            structName: "TestStruct",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestStruct",
            accessModifier: "public",
            isReadOnly: true,
            isRef: false,
            interfaces: interfaces,
            methodCount: 3,
            propertyCount: 2,
            fieldCount: 5,
            filePath: "TestStruct.cs",
            lineNumber: 15
        );

        // Act
        var result = structDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("readonly", result);
        Assert.Contains("struct", result);
        Assert.Contains("TestStruct", result);
        Assert.Contains("IDisposable", result);
        Assert.Contains("line 15", result);
        Assert.Contains("TestStruct.cs", result);
    }

    [Fact]
    public void ToString_WithRefStruct_ReturnsFormattedString()
    {
        // Arrange
        var structDef = new StructDefinitionInfo(
            structName: "RefStruct",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.RefStruct",
            accessModifier: "public",
            isReadOnly: false,
            isRef: true,
            interfaces: new List<string>(),
            methodCount: 0,
            propertyCount: 0,
            fieldCount: 0,
            filePath: "RefStruct.cs",
            lineNumber: 1
        );

        // Act
        var result = structDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("ref", result);
        Assert.Contains("struct", result);
        Assert.Contains("RefStruct", result);
        Assert.DoesNotContain("readonly", result);
    }

    [Fact]
    public void ToString_WithNoModifiers_ReturnsFormattedString()
    {
        // Arrange
        var structDef = new StructDefinitionInfo(
            structName: "SimpleStruct",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.SimpleStruct",
            accessModifier: "internal",
            isReadOnly: false,
            isRef: false,
            interfaces: new List<string>(),
            methodCount: 1,
            propertyCount: 1,
            fieldCount: 2,
            filePath: "SimpleStruct.cs",
            lineNumber: 5
        );

        // Act
        var result = structDef.ToString();

        // Assert
        Assert.Contains("internal", result);
        Assert.Contains("struct", result);
        Assert.Contains("SimpleStruct", result);
        Assert.Contains("line 5", result);
        Assert.DoesNotContain("readonly", result);
        Assert.DoesNotContain("ref", result);
    }
}

