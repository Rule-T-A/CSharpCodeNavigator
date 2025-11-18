using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerStructDefinitionsTests
{
    [Fact]
    public void ExtractStructDefinitions_SimpleStruct_ReturnsStructDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct TestStruct
    {
        public int Field1;
        public string Property1 { get; set; }
        public void Method1() { }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        Assert.Single(structDefinitions);
        var structDef = structDefinitions[0];
        Assert.Equal("TestStruct", structDef.StructName);
        Assert.Equal("TestNamespace", structDef.Namespace);
        Assert.Equal("TestNamespace.TestStruct", structDef.FullyQualifiedName);
        Assert.Equal("public", structDef.AccessModifier);
        Assert.False(structDef.IsReadOnly);
        Assert.False(structDef.IsRef);
        Assert.Equal(1, structDef.MethodCount);
        Assert.Equal(1, structDef.PropertyCount);
        Assert.Equal(1, structDef.FieldCount);
        Assert.Empty(structDef.Interfaces);
        Assert.True(structDef.LineNumber > 0);
    }

    [Fact]
    public void ExtractStructDefinitions_ReadOnlyStruct_ReturnsStructDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public readonly struct TestStruct
    {
        public int Field1;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        Assert.Single(structDefinitions);
        var structDef = structDefinitions[0];
        Assert.True(structDef.IsReadOnly);
        Assert.False(structDef.IsRef);
    }

    [Fact]
    public void ExtractStructDefinitions_RefStruct_ReturnsStructDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public ref struct TestStruct
    {
        public int Field1;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        Assert.Single(structDefinitions);
        var structDef = structDefinitions[0];
        Assert.False(structDef.IsReadOnly);
        Assert.True(structDef.IsRef);
    }

    [Fact]
    public void ExtractStructDefinitions_StructWithInterfaces_ReturnsStructDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IDisposable { }
    public interface IComparable { }
    
    public struct TestStruct : IDisposable, IComparable
    {
        public void Method1() { }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        var structDef = structDefinitions.First(s => s.StructName == "TestStruct");
        Assert.Equal(2, structDef.Interfaces.Count);
        Assert.Contains("TestNamespace.IDisposable", structDef.Interfaces);
        Assert.Contains("TestNamespace.IComparable", structDef.Interfaces);
    }

    [Fact]
    public void ExtractStructDefinitions_MultipleStructs_ReturnsAllStructs()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct Struct1 { }
    internal struct Struct2 { }
    public readonly struct Struct3 { }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        Assert.Equal(3, structDefinitions.Count);
        Assert.Contains(structDefinitions, s => s.StructName == "Struct1" && s.AccessModifier == "public" && !s.IsReadOnly);
        Assert.Contains(structDefinitions, s => s.StructName == "Struct2" && s.AccessModifier == "internal");
        Assert.Contains(structDefinitions, s => s.StructName == "Struct3" && s.IsReadOnly);
    }

    [Fact]
    public void ExtractStructDefinitions_StructWithMultipleFields_ReturnsCorrectFieldCount()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct TestStruct
    {
        public int Field1;
        public string Field2;
        public bool Field3;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        Assert.Single(structDefinitions);
        var structDef = structDefinitions[0];
        Assert.Equal(3, structDef.FieldCount);
    }

    [Fact]
    public void ExtractStructDefinitions_NoStructs_ReturnsEmptyList()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass { }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var structDefinitions = analyzer.ExtractStructDefinitions(tree, model);

        // Assert
        Assert.Empty(structDefinitions);
    }
}

