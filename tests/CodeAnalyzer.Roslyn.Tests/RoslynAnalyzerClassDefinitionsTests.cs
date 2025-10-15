using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerClassDefinitionsTests
{
    private async Task<Compilation> CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
        };
        return CSharpCompilation.Create("TestAssembly", new[] { tree }, references);
    }

    [Fact]
    public async Task ExtractClassDefinitions_SimpleClass_ReturnsClassDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public string GetName()
        {
            return ""Test"";
        }
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Single(classDefinitions);
        var classDef = classDefinitions.First();
        Assert.Equal("TestClass", classDef.ClassName);
        Assert.Equal("TestNamespace", classDef.Namespace);
        Assert.Equal("TestNamespace.TestClass", classDef.FullyQualifiedName);
        Assert.Equal("public", classDef.AccessModifier);
        Assert.False(classDef.IsStatic);
        Assert.False(classDef.IsAbstract);
        Assert.False(classDef.IsSealed);
        Assert.Equal(string.Empty, classDef.BaseClass);
        Assert.Empty(classDef.Interfaces);
        Assert.True(classDef.LineNumber > 0);
        Assert.True(classDef.MethodCount > 0); // Should have at least one method
    }

    [Fact]
    public async Task ExtractClassDefinitions_AbstractClass_ReturnsCorrectModifiers()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public abstract class AbstractClass
    {
        public abstract void AbstractMethod();
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Single(classDefinitions);
        var classDef = classDefinitions.First();
        Assert.Equal("AbstractClass", classDef.ClassName);
        Assert.True(classDef.IsAbstract);
        Assert.False(classDef.IsStatic);
        Assert.False(classDef.IsSealed);
    }

    [Fact]
    public async Task ExtractClassDefinitions_StaticClass_ReturnsCorrectModifiers()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public static class StaticClass
    {
        public static void StaticMethod()
        {
        }
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Single(classDefinitions);
        var classDef = classDefinitions.First();
        Assert.Equal("StaticClass", classDef.ClassName);
        Assert.True(classDef.IsStatic);
        Assert.False(classDef.IsAbstract);
        Assert.False(classDef.IsSealed);
    }

    [Fact]
    public async Task ExtractClassDefinitions_SealedClass_ReturnsCorrectModifiers()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public sealed class SealedClass
    {
        public void Method()
        {
        }
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Single(classDefinitions);
        var classDef = classDefinitions.First();
        Assert.Equal("SealedClass", classDef.ClassName);
        Assert.True(classDef.IsSealed);
        Assert.False(classDef.IsAbstract);
        Assert.False(classDef.IsStatic);
    }

    [Fact]
    public async Task ExtractClassDefinitions_ClassWithInheritance_ReturnsInheritanceInfo()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class BaseClass
    {
    }

    public interface IInterface
    {
    }

    public class DerivedClass : BaseClass, IInterface
    {
        public void Method()
        {
        }
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Equal(2, classDefinitions.Count);
        
        var derivedClass = classDefinitions.First(c => c.ClassName == "DerivedClass");
        Assert.Equal("TestNamespace.BaseClass", derivedClass.BaseClass);
        Assert.Single(derivedClass.Interfaces);
        Assert.Contains("TestNamespace.IInterface", derivedClass.Interfaces);
    }

    [Fact]
    public async Task ExtractClassDefinitions_ClassWithMembers_ReturnsCorrectCounts()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        private string _field;
        
        public string Property { get; set; }
        
        public void Method1()
        {
        }
        
        public void Method2()
        {
        }
        
        public string Property2 { get; set; }
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Single(classDefinitions);
        var classDef = classDefinitions.First();
        Assert.Equal(2, classDef.MethodCount);
        Assert.Equal(2, classDef.PropertyCount);
        Assert.Equal(1, classDef.FieldCount);
    }

    [Fact]
    public async Task ExtractClassDefinitions_PrivateClass_ReturnsCorrectAccessModifier()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    private class PrivateClass
    {
        public void Method()
        {
        }
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Single(classDefinitions);
        var classDef = classDefinitions.First();
        Assert.Equal("PrivateClass", classDef.ClassName);
        Assert.Equal("private", classDef.AccessModifier);
    }

    [Fact]
    public async Task ExtractClassDefinitions_NoClasses_ReturnsEmptyList()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public interface IInterface
    {
        void Method();
    }
}";
        var compilation = await CreateCompilation(source);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var analyzer = new RoslynAnalyzer();

        // Act
        var classDefinitions = analyzer.ExtractClassDefinitions(tree, model);

        // Assert
        Assert.Empty(classDefinitions);
    }

    [Fact]
    public async Task ExtractClassDefinitions_NullTree_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var compilation = await CreateCompilation("public class Test { }");
        var model = compilation.GetSemanticModel(compilation.SyntaxTrees.First());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => analyzer.ExtractClassDefinitions(null!, model));
    }

    [Fact]
    public async Task ExtractClassDefinitions_NullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var compilation = await CreateCompilation("public class Test { }");
        var tree = compilation.SyntaxTrees.First();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => analyzer.ExtractClassDefinitions(tree, null!));
    }
}
