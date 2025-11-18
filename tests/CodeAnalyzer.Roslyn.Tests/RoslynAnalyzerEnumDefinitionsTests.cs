using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerEnumDefinitionsTests
{
    [Fact]
    public void ExtractEnumDefinitions_SimpleEnum_ReturnsEnumDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Single(enumDefinitions);
        var enumDef = enumDefinitions[0];
        Assert.Equal("TestEnum", enumDef.EnumName);
        Assert.Equal("TestNamespace", enumDef.Namespace);
        Assert.Equal("TestNamespace.TestEnum", enumDef.FullyQualifiedName);
        Assert.Equal("public", enumDef.AccessModifier);
        Assert.Equal("int", enumDef.UnderlyingType);
        Assert.Equal(3, enumDef.Values.Count);
        Assert.Contains(enumDef.Values, v => v.ValueName == "Value1");
        Assert.Contains(enumDef.Values, v => v.ValueName == "Value2");
        Assert.Contains(enumDef.Values, v => v.ValueName == "Value3");
        Assert.True(enumDef.LineNumber > 0);
    }

    [Fact]
    public void ExtractEnumDefinitions_EnumWithExplicitValues_ReturnsEnumDefinitionWithValues()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum
    {
        Value1 = 10,
        Value2 = 20,
        Value3 = 30
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Single(enumDefinitions);
        var enumDef = enumDefinitions[0];
        Assert.Equal(3, enumDef.Values.Count);
        
        var value1 = enumDef.Values.First(v => v.ValueName == "Value1");
        Assert.Equal(10, value1.Value);
        
        var value2 = enumDef.Values.First(v => v.ValueName == "Value2");
        Assert.Equal(20, value2.Value);
        
        var value3 = enumDef.Values.First(v => v.ValueName == "Value3");
        Assert.Equal(30, value3.Value);
    }

    [Fact]
    public void ExtractEnumDefinitions_EnumWithByteUnderlyingType_ReturnsEnumDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum : byte
    {
        Value1,
        Value2
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Single(enumDefinitions);
        var enumDef = enumDefinitions[0];
        Assert.Equal("byte", enumDef.UnderlyingType);
    }

    [Fact]
    public void ExtractEnumDefinitions_PrivateEnum_ReturnsEnumDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    private enum TestEnum
    {
        Value1
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Single(enumDefinitions);
        var enumDef = enumDefinitions[0];
        Assert.Equal("private", enumDef.AccessModifier);
    }

    [Fact]
    public void ExtractEnumDefinitions_MultipleEnums_ReturnsAllEnums()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum Enum1
    {
        Value1
    }

    public enum Enum2
    {
        Value2
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Equal(2, enumDefinitions.Count);
        Assert.Contains(enumDefinitions, e => e.EnumName == "Enum1");
        Assert.Contains(enumDefinitions, e => e.EnumName == "Enum2");
    }

    [Fact]
    public void ExtractEnumDefinitions_NoEnums_ReturnsEmptyList()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void Method() { }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Empty(enumDefinitions);
    }

    [Fact]
    public void ExtractEnumDefinitions_EnumWithLongUnderlyingType_ReturnsEnumDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum : long
    {
        Value1 = 1000000L
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var enumDefinitions = analyzer.ExtractEnumDefinitions(tree, model);

        // Assert
        Assert.Single(enumDefinitions);
        var enumDef = enumDefinitions[0];
        Assert.Equal("long", enumDef.UnderlyingType);
    }
}

