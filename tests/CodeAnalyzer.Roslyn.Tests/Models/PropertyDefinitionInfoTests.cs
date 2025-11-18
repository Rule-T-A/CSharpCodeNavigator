using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class PropertyDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var propertyDef = new PropertyDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, propertyDef.PropertyName);
        Assert.Equal(string.Empty, propertyDef.ClassName);
        Assert.Equal(string.Empty, propertyDef.Namespace);
        Assert.Equal(string.Empty, propertyDef.FullyQualifiedName);
        Assert.Equal(string.Empty, propertyDef.PropertyType);
        Assert.Equal(string.Empty, propertyDef.AccessModifier);
        Assert.False(propertyDef.IsStatic);
        Assert.False(propertyDef.IsVirtual);
        Assert.False(propertyDef.IsAbstract);
        Assert.False(propertyDef.IsOverride);
        Assert.False(propertyDef.HasGetter);
        Assert.False(propertyDef.HasSetter);
        Assert.False(propertyDef.IsAutoProperty);
        Assert.Equal(string.Empty, propertyDef.FilePath);
        Assert.Equal(0, propertyDef.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Act
        var propertyDef = new PropertyDefinitionInfo(
            propertyName: "TestProperty",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestProperty",
            propertyType: "string",
            accessModifier: "public",
            isStatic: true,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            hasGetter: true,
            hasSetter: true,
            isAutoProperty: true,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Assert
        Assert.Equal("TestProperty", propertyDef.PropertyName);
        Assert.Equal("TestClass", propertyDef.ClassName);
        Assert.Equal("TestNamespace", propertyDef.Namespace);
        Assert.Equal("TestNamespace.TestClass.TestProperty", propertyDef.FullyQualifiedName);
        Assert.Equal("string", propertyDef.PropertyType);
        Assert.Equal("public", propertyDef.AccessModifier);
        Assert.True(propertyDef.IsStatic);
        Assert.False(propertyDef.IsVirtual);
        Assert.False(propertyDef.IsAbstract);
        Assert.False(propertyDef.IsOverride);
        Assert.True(propertyDef.HasGetter);
        Assert.True(propertyDef.HasSetter);
        Assert.True(propertyDef.IsAutoProperty);
        Assert.Equal("TestClass.cs", propertyDef.FilePath);
        Assert.Equal(42, propertyDef.LineNumber);
    }

    [Fact]
    public void ToString_WithAllProperties_ReturnsFormattedString()
    {
        // Arrange
        var propertyDef = new PropertyDefinitionInfo(
            propertyName: "TestProperty",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.TestProperty",
            propertyType: "string",
            accessModifier: "public",
            isStatic: true,
            isVirtual: true,
            isAbstract: false,
            isOverride: false,
            hasGetter: true,
            hasSetter: true,
            isAutoProperty: false,
            filePath: "TestClass.cs",
            lineNumber: 42
        );

        // Act
        var result = propertyDef.ToString();

        // Assert
        Assert.Contains("public", result);
        Assert.Contains("static", result);
        Assert.Contains("virtual", result);
        Assert.Contains("string", result);
        Assert.Contains("TestProperty", result);
        Assert.Contains("{ get; set; }", result);
        Assert.Contains("line 42", result);
        Assert.Contains("TestClass.cs", result);
    }

    [Fact]
    public void ToString_WithGetterOnly_ReturnsFormattedString()
    {
        // Arrange
        var propertyDef = new PropertyDefinitionInfo(
            propertyName: "ReadOnlyProperty",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.ReadOnlyProperty",
            propertyType: "int",
            accessModifier: "private",
            isStatic: false,
            isVirtual: false,
            isAbstract: false,
            isOverride: false,
            hasGetter: true,
            hasSetter: false,
            isAutoProperty: true,
            filePath: "TestClass.cs",
            lineNumber: 10
        );

        // Act
        var result = propertyDef.ToString();

        // Assert
        Assert.Contains("private", result);
        Assert.Contains("int", result);
        Assert.Contains("ReadOnlyProperty", result);
        Assert.Contains("{ get; }", result);
        Assert.Contains("line 10", result);
    }

    [Fact]
    public void ToString_WithAbstractOverride_ReturnsFormattedString()
    {
        // Arrange
        var propertyDef = new PropertyDefinitionInfo(
            propertyName: "AbstractProperty",
            className: "TestClass",
            namespaceName: "TestNamespace",
            fullyQualifiedName: "TestNamespace.TestClass.AbstractProperty",
            propertyType: "object",
            accessModifier: "protected",
            isStatic: false,
            isVirtual: false,
            isAbstract: true,
            isOverride: true,
            hasGetter: true,
            hasSetter: false,
            isAutoProperty: false,
            filePath: "TestClass.cs",
            lineNumber: 20
        );

        // Act
        var result = propertyDef.ToString();

        // Assert
        Assert.Contains("protected", result);
        Assert.Contains("abstract", result);
        Assert.Contains("override", result);
        Assert.Contains("object", result);
        Assert.Contains("AbstractProperty", result);
        Assert.Contains("{ get; }", result);
    }
}

