using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class FieldDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var fieldDef = new FieldDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, fieldDef.FieldName);
        Assert.Equal(string.Empty, fieldDef.ClassName);
        Assert.Equal(string.Empty, fieldDef.Namespace);
        Assert.Equal(string.Empty, fieldDef.FullyQualifiedName);
        Assert.Equal(string.Empty, fieldDef.FieldType);
        Assert.Equal(string.Empty, fieldDef.AccessModifier);
        Assert.False(fieldDef.IsStatic);
        Assert.False(fieldDef.IsReadOnly);
        Assert.False(fieldDef.IsConst);
        Assert.False(fieldDef.IsVolatile);
        Assert.Equal(string.Empty, fieldDef.FilePath);
        Assert.Equal(0, fieldDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Act
        var fieldDef = new FieldDefinitionInfo(
            fieldName: "TestField",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestField",
            fieldType: "string",
            accessModifier: "public",
            isStatic: true,
            isReadOnly: false,
            isConst: false,
            isVolatile: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Assert
        Assert.Equal("TestField", fieldDef.FieldName);
        Assert.Equal("TestClass", fieldDef.ClassName);
        Assert.Equal("TestNamespace", fieldDef.Namespace);
        Assert.Equal("TestNamespace.TestClass.TestField", fieldDef.FullyQualifiedName);
        Assert.Equal("string", fieldDef.FieldType);
        Assert.Equal("public", fieldDef.AccessModifier);
        Assert.True(fieldDef.IsStatic);
        Assert.False(fieldDef.IsReadOnly);
        Assert.False(fieldDef.IsConst);
        Assert.False(fieldDef.IsVolatile);
        Assert.Equal("TestClass.cs", fieldDef.FilePath);
        Assert.Equal(42, fieldDef.LineNumber);
    }

    [Fact]
    public void ToString_WithAllProperties_ReturnsFormattedString()
    {
        // Arrange
        var fieldDef = new FieldDefinitionInfo(
            fieldName: "TestField",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestField",
            fieldType: "string",
            accessModifier: "public",
            isStatic: true,
            isReadOnly: true,
            isConst: false,
            isVolatile: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Act
        var result = fieldDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("static", result);
        Assert.Contains("readonly", result);
        Assert.Contains("string", result);
        Assert.Contains("TestField", result);
        Assert.Contains("line 42", result);
        Assert.Contains("TestClass.cs", result);
    }

    [Fact]
    public void ToString_WithConst_ReturnsFormattedString()
    {
        // Arrange
        var fieldDef = new FieldDefinitionInfo(
            fieldName: "ConstField",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.ConstField",
            fieldType: "int",
            accessModifier: "private",
            isStatic: false,
            isReadOnly: false,
            isConst: true,
            isVolatile: false,
            filePath: "TestClass.cs",
            lineNumber: 10
        );

        // Act
        var result = fieldDef.ToString();

        // Assert
        Assert.Contains("private", result);
        Assert.Contains("const", result);
        Assert.Contains("int", result);
        Assert.Contains("ConstField", result);
        Assert.Contains("line 10", result);
    }

    [Fact]
    public void ToString_WithVolatile_ReturnsFormattedString()
    {
        // Arrange
        var fieldDef = new FieldDefinitionInfo(
            fieldName: "VolatileField",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.VolatileField",
            fieldType: "bool",
            accessModifier: "internal",
            isStatic: false,
            isReadOnly: false,
            isConst: false,
            isVolatile: true,
            filePath: "TestClass.cs",
            lineNumber: 20
        );

        // Act
        var result = fieldDef.ToString();

        // Assert
        Assert.Contains("internal", result);
        Assert.Contains("volatile", result);
        Assert.Contains("bool", result);
        Assert.Contains("VolatileField", result);
        Assert.Contains("line 20", result);
    }

    [Fact]
    public void ToString_WithMinimalProperties_ReturnsFormattedString()
    {
        // Arrange
        var fieldDef = new FieldDefinitionInfo(
            fieldName: "SimpleField",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.SimpleField",
            fieldType: "object",
            accessModifier: "private",
            isStatic: false,
            isReadOnly: false,
            isConst: false,
            isVolatile: false,
            filePath: "TestClass.cs",
            lineNumber: 1
        );

        // Act
        var result = fieldDef.ToString();

        // Assert
        Assert.Contains("private", result);
        Assert.Contains("object", result);
        Assert.Contains("SimpleField", result);
        Assert.Contains("line 1", result);
        // Should not contain modifier keywords
        Assert.DoesNotContain("static", result);
        Assert.DoesNotContain("readonly", result);
        Assert.DoesNotContain("const", result);
        Assert.DoesNotContain("volatile", result);
    }
}

