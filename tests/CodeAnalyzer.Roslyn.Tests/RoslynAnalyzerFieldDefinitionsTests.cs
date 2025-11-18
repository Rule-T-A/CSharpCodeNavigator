using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerFieldDefinitionsTests
{
    [Fact]
    public void ExtractFieldDefinitions_SimpleField_ReturnsFieldDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public string Name;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Single(fieldDefinitions);
        var fieldDef = fieldDefinitions[0];
        Assert.Equal("Name", fieldDef.FieldName);
        Assert.Equal("TestClass", fieldDef.ClassName);
        Assert.Equal("TestNamespace", fieldDef.Namespace);
        Assert.Equal("TestNamespace.TestClass.Name", fieldDef.FullyQualifiedName);
        Assert.Equal("string", fieldDef.FieldType);
        Assert.Equal("public", fieldDef.AccessModifier);
        Assert.False(fieldDef.IsStatic);
        Assert.False(fieldDef.IsReadOnly);
        Assert.False(fieldDef.IsConst);
        Assert.False(fieldDef.IsVolatile);
        Assert.True(fieldDef.LineNumber > 0);
    }

    [Fact]
    public void ExtractFieldDefinitions_StaticField_ReturnsFieldDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public static string StaticField;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Single(fieldDefinitions);
        var fieldDef = fieldDefinitions[0];
        Assert.Equal("StaticField", fieldDef.FieldName);
        Assert.True(fieldDef.IsStatic);
    }

    [Fact]
    public void ExtractFieldDefinitions_ReadOnlyField_ReturnsFieldDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public readonly int ReadOnlyField;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Single(fieldDefinitions);
        var fieldDef = fieldDefinitions[0];
        Assert.Equal("ReadOnlyField", fieldDef.FieldName);
        Assert.True(fieldDef.IsReadOnly);
    }

    [Fact]
    public void ExtractFieldDefinitions_ConstField_ReturnsFieldDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public const int ConstField = 42;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Single(fieldDefinitions);
        var fieldDef = fieldDefinitions[0];
        Assert.Equal("ConstField", fieldDef.FieldName);
        Assert.True(fieldDef.IsConst);
    }

    [Fact]
    public void ExtractFieldDefinitions_VolatileField_ReturnsFieldDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public volatile bool VolatileField;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Single(fieldDefinitions);
        var fieldDef = fieldDefinitions[0];
        Assert.Equal("VolatileField", fieldDef.FieldName);
        Assert.True(fieldDef.IsVolatile);
    }

    [Fact]
    public void ExtractFieldDefinitions_MultipleFieldsInOneDeclaration_ReturnsAllFields()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int x, y, z;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Equal(3, fieldDefinitions.Count);
        Assert.Contains(fieldDefinitions, f => f.FieldName == "x");
        Assert.Contains(fieldDefinitions, f => f.FieldName == "y");
        Assert.Contains(fieldDefinitions, f => f.FieldName == "z");
        // All should have the same type and modifiers
        Assert.All(fieldDefinitions, f => Assert.Equal("int", f.FieldType));
        Assert.All(fieldDefinitions, f => Assert.Equal("public", f.AccessModifier));
    }

    [Fact]
    public void ExtractFieldDefinitions_MultipleFields_ReturnsAllFields()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Field1;
        private int Field2;
        protected bool Field3;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Equal(3, fieldDefinitions.Count);
        Assert.Contains(fieldDefinitions, f => f.FieldName == "Field1" && f.AccessModifier == "public");
        Assert.Contains(fieldDefinitions, f => f.FieldName == "Field2" && f.AccessModifier == "private");
        Assert.Contains(fieldDefinitions, f => f.FieldName == "Field3" && f.AccessModifier == "protected");
    }

    [Fact]
    public void ExtractFieldDefinitions_NoFields_ReturnsEmptyList()
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
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Empty(fieldDefinitions);
    }

    [Fact]
    public void ExtractFieldDefinitions_PrivateField_ReturnsFieldDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        private string _privateField;
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var fieldDefinitions = analyzer.ExtractFieldDefinitions(tree, model);

        // Assert
        Assert.Single(fieldDefinitions);
        var fieldDef = fieldDefinitions[0];
        Assert.Equal("_privateField", fieldDef.FieldName);
        Assert.Equal("private", fieldDef.AccessModifier);
    }
}

