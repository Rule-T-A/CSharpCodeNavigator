using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerInterfaceDefinitionsTests
{
    [Fact]
    public void ExtractInterfaceDefinitions_SimpleInterface_ReturnsInterfaceDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface ITestInterface
    {
        void Method1();
        int Property1 { get; set; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var interfaceDefinitions = analyzer.ExtractInterfaceDefinitions(tree, model);

        // Assert
        Assert.Single(interfaceDefinitions);
        var interfaceDef = interfaceDefinitions[0];
        Assert.Equal("ITestInterface", interfaceDef.InterfaceName);
        Assert.Equal("TestNamespace", interfaceDef.Namespace);
        Assert.Equal("TestNamespace.ITestInterface", interfaceDef.FullyQualifiedName);
        Assert.Equal("public", interfaceDef.AccessModifier);
        Assert.Equal(1, interfaceDef.MethodCount);
        Assert.Equal(1, interfaceDef.PropertyCount);
        Assert.Empty(interfaceDef.BaseInterfaces);
        Assert.True(interfaceDef.LineNumber > 0);
    }

    [Fact]
    public void ExtractInterfaceDefinitions_InterfaceWithBaseInterfaces_ReturnsInterfaceDefinition()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IBaseInterface1 { }
    public interface IBaseInterface2 { }
    
    public interface ITestInterface : IBaseInterface1, IBaseInterface2
    {
        void Method1();
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var interfaceDefinitions = analyzer.ExtractInterfaceDefinitions(tree, model);

        // Assert
        Assert.Equal(3, interfaceDefinitions.Count);
        var interfaceDef = interfaceDefinitions.First(i => i.InterfaceName == "ITestInterface");
        Assert.Equal(2, interfaceDef.BaseInterfaces.Count);
        Assert.Contains("TestNamespace.IBaseInterface1", interfaceDef.BaseInterfaces);
        Assert.Contains("TestNamespace.IBaseInterface2", interfaceDef.BaseInterfaces);
    }

    [Fact]
    public void ExtractInterfaceDefinitions_MultipleInterfaces_ReturnsAllInterfaces()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IInterface1 { }
    internal interface IInterface2 { }
    public interface IInterface3 { }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var interfaceDefinitions = analyzer.ExtractInterfaceDefinitions(tree, model);

        // Assert
        Assert.Equal(3, interfaceDefinitions.Count);
        Assert.Contains(interfaceDefinitions, i => i.InterfaceName == "IInterface1" && i.AccessModifier == "public");
        Assert.Contains(interfaceDefinitions, i => i.InterfaceName == "IInterface2" && i.AccessModifier == "internal");
        Assert.Contains(interfaceDefinitions, i => i.InterfaceName == "IInterface3" && i.AccessModifier == "public");
    }

    [Fact]
    public void ExtractInterfaceDefinitions_InterfaceWithOnlyMethods_ReturnsCorrectCounts()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface ITestInterface
    {
        void Method1();
        int Method2();
        string Method3();
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var interfaceDefinitions = analyzer.ExtractInterfaceDefinitions(tree, model);

        // Assert
        Assert.Single(interfaceDefinitions);
        var interfaceDef = interfaceDefinitions[0];
        Assert.Equal(3, interfaceDef.MethodCount);
        Assert.Equal(0, interfaceDef.PropertyCount);
    }

    [Fact]
    public void ExtractInterfaceDefinitions_InterfaceWithOnlyProperties_ReturnsCorrectCounts()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface ITestInterface
    {
        int Property1 { get; set; }
        string Property2 { get; }
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var interfaceDefinitions = analyzer.ExtractInterfaceDefinitions(tree, model);

        // Assert
        Assert.Single(interfaceDefinitions);
        var interfaceDef = interfaceDefinitions[0];
        Assert.Equal(0, interfaceDef.MethodCount);
        Assert.Equal(2, interfaceDef.PropertyCount);
    }

    [Fact]
    public void ExtractInterfaceDefinitions_NoInterfaces_ReturnsEmptyList()
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
        var interfaceDefinitions = analyzer.ExtractInterfaceDefinitions(tree, model);

        // Assert
        Assert.Empty(interfaceDefinitions);
    }
}

