using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerPropertyDefinitionsTests
{
    [Fact]
    public void ExtractPropertyDefinitions_SimpleProperty_ReturnsPropertyDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public string Name { get; set; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Single(propertyDefinitions);
        var propertyDef = propertyDefinitions[0];
        Assert.Equal("Name", propertyDef.PropertyName);
        Assert.Equal("TestClass", propertyDef.ClassName);
        Assert.Equal("TestNamespace", propertyDef.Namespace);
        Assert.Equal("TestNamespace.TestClass.Name", propertyDef.FullyQualifiedName);
        Assert.Equal("string", propertyDef.PropertyType);
        Assert.Equal("public", propertyDef.AccessModifier);
        Assert.False(propertyDef.IsStatic);
        Assert.False(propertyDef.IsVirtual);
        Assert.False(propertyDef.IsAbstract);
        Assert.False(propertyDef.IsOverride);
        Assert.True(propertyDef.HasGetter);
        Assert.True(propertyDef.HasSetter);
        Assert.True(propertyDef.IsAutoProperty);
        Assert.True(propertyDef.LineNumber > 0);
    }

    [Fact]
    public void ExtractPropertyDefinitions_ReadOnlyProperty_ReturnsPropertyDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public int Value { get; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Single(propertyDefinitions);
        var propertyDef = propertyDefinitions[0];
        Assert.Equal("Value", propertyDef.PropertyName);
        Assert.True(propertyDef.HasGetter);
        Assert.False(propertyDef.HasSetter);
    }

    [Fact]
    public void ExtractPropertyDefinitions_StaticProperty_ReturnsPropertyDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public static string StaticProperty { get; set; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Single(propertyDefinitions);
        var propertyDef = propertyDefinitions[0];
        Assert.Equal("StaticProperty", propertyDef.PropertyName);
        Assert.True(propertyDef.IsStatic);
    }

    [Fact]
    public void ExtractPropertyDefinitions_VirtualProperty_ReturnsPropertyDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public virtual string VirtualProperty { get; set; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Single(propertyDefinitions);
        var propertyDef = propertyDefinitions[0];
        Assert.Equal("VirtualProperty", propertyDef.PropertyName);
        Assert.True(propertyDef.IsVirtual);
    }

    [Fact]
    public void ExtractPropertyDefinitions_AbstractProperty_ReturnsPropertyDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public abstract class TestClass
    {
        public abstract string AbstractProperty { get; set; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Single(propertyDefinitions);
        var propertyDef = propertyDefinitions[0];
        Assert.Equal("AbstractProperty", propertyDef.PropertyName);
        Assert.True(propertyDef.IsAbstract);
    }

    [Fact]
    public void ExtractPropertyDefinitions_PropertyWithAccessors_ReturnsPropertyDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        private string _name;
        public string Name 
        { 
            get { return _name; }
            set { _name = value; }
        }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Single(propertyDefinitions);
        var propertyDef = propertyDefinitions[0];
        Assert.Equal("Name", propertyDef.PropertyName);
        Assert.True(propertyDef.HasGetter);
        Assert.True(propertyDef.HasSetter);
        Assert.False(propertyDef.IsAutoProperty); // Has accessor bodies
    }

    [Fact]
    public void ExtractPropertyDefinitions_MultipleProperties_ReturnsAllProperties()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Property1 { get; set; }
        public int Property2 { get; }
        private bool Property3 { get; set; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Equal(3, propertyDefinitions.Count);
        Assert.Contains(propertyDefinitions, p => p.PropertyName == "Property1");
        Assert.Contains(propertyDefinitions, p => p.PropertyName == "Property2");
        Assert.Contains(propertyDefinitions, p => p.PropertyName == "Property3");
    }

    [Fact]
    public void ExtractPropertyDefinitions_NoProperties_ReturnsEmptyList()
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
        var propertyDefinitions = analyzer.ExtractPropertyDefinitions(tree, model);

        // Assert
        Assert.Empty(propertyDefinitions);
    }
}

