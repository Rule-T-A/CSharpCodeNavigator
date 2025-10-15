using CodeAnalyzer.Roslyn.Models;
using Xunit;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class ClassDefinitionInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var classDef = new ClassDefinitionInfo();

        // Assert
        Assert.Equal(string.Empty, classDef.ClassName);
        Assert.Equal(string.Empty, classDef.Namespace);
        Assert.Equal(string.Empty, classDef.FullyQualifiedName);
        Assert.Equal("private", classDef.AccessModifier);
        Assert.False(classDef.IsStatic);
        Assert.False(classDef.IsAbstract);
        Assert.False(classDef.IsSealed);
        Assert.Equal(string.Empty, classDef.BaseClass);
        Assert.NotNull(classDef.Interfaces);
        Assert.Empty(classDef.Interfaces);
        Assert.Equal(string.Empty, classDef.FilePath);
        Assert.Equal(0, classDef.LineNumber);
        Assert.Equal(0, classDef.MethodCount);
        Assert.Equal(0, classDef.PropertyCount);
        Assert.Equal(0, classDef.FieldCount);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var className = "TestClass";
        var namespaceName = "TestNamespace";
        var fullyQualifiedName = "TestNamespace.TestClass";
        var accessModifier = "public";
        var isStatic = true;
        var isAbstract = false;
        var isSealed = true;
        var baseClass = "BaseClass";
        var interfaces = new List<string> { "IInterface1", "IInterface2" };
        var filePath = "TestFile.cs";
        var lineNumber = 10;
        var methodCount = 5;
        var propertyCount = 3;
        var fieldCount = 2;

        // Act
        var classDef = new ClassDefinitionInfo(
            className, namespaceName, fullyQualifiedName, accessModifier,
            isStatic, isAbstract, isSealed, baseClass, interfaces,
            filePath, lineNumber, methodCount, propertyCount, fieldCount);

        // Assert
        Assert.Equal(className, classDef.ClassName);
        Assert.Equal(namespaceName, classDef.Namespace);
        Assert.Equal(fullyQualifiedName, classDef.FullyQualifiedName);
        Assert.Equal(accessModifier, classDef.AccessModifier);
        Assert.Equal(isStatic, classDef.IsStatic);
        Assert.Equal(isAbstract, classDef.IsAbstract);
        Assert.Equal(isSealed, classDef.IsSealed);
        Assert.Equal(baseClass, classDef.BaseClass);
        Assert.Equal(interfaces, classDef.Interfaces);
        Assert.Equal(filePath, classDef.FilePath);
        Assert.Equal(lineNumber, classDef.LineNumber);
        Assert.Equal(methodCount, classDef.MethodCount);
        Assert.Equal(propertyCount, classDef.PropertyCount);
        Assert.Equal(fieldCount, classDef.FieldCount);
    }

    [Fact]
    public void Constructor_WithNullInterfaces_InitializesEmptyList()
    {
        // Arrange & Act
        var classDef = new ClassDefinitionInfo(
            "TestClass", "TestNamespace", "TestNamespace.TestClass", "public",
            false, false, false, "", null, "TestFile.cs", 1, 0, 0, 0);

        // Assert
        Assert.NotNull(classDef.Interfaces);
        Assert.Empty(classDef.Interfaces);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var classDef = new ClassDefinitionInfo(
            "TestClass", "TestNamespace", "TestNamespace.TestClass", "public",
            true, false, true, "BaseClass", new List<string> { "IInterface1" },
            "TestFile.cs", 10, 5, 3, 2);

        // Act
        var result = classDef.ToString();

        // Assert
        Assert.Contains("public static sealed TestClass : BaseClass, IInterface1", result);
        Assert.Contains("(line 10 in TestFile.cs)", result);
    }

    [Fact]
    public void ToString_NoInheritance_ReturnsFormattedString()
    {
        // Arrange
        var classDef = new ClassDefinitionInfo(
            "TestClass", "TestNamespace", "TestNamespace.TestClass", "private",
            false, true, false, "", new List<string>(),
            "TestFile.cs", 5, 0, 0, 0);

        // Act
        var result = classDef.ToString();

        // Assert
        Assert.Contains("private abstract TestClass", result);
        Assert.Contains("(line 5 in TestFile.cs)", result);
        Assert.DoesNotContain(":", result);
    }
}
