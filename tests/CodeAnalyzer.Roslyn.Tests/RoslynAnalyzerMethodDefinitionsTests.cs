using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeAnalyzer.Roslyn.Tests;

public class RoslynAnalyzerMethodDefinitionsTests
{
    [Fact]
    public void ExtractMethodDefinitions_SimpleMethod_ReturnsMethodDefinition()
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
            return ""test"";
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("GetName", methodDef.MethodName);
        Assert.Equal("TestClass", methodDef.ClassName);
        Assert.Equal("TestNamespace", methodDef.Namespace);
        Assert.Equal("TestNamespace.TestClass.GetName", methodDef.FullyQualifiedName);
        Assert.Equal("string", methodDef.ReturnType);
        Assert.Empty(methodDef.Parameters);
        Assert.Equal("public", methodDef.AccessModifier);
        Assert.False(methodDef.IsStatic);
        Assert.False(methodDef.IsVirtual);
        Assert.False(methodDef.IsAbstract);
        Assert.False(methodDef.IsOverride);
        Assert.True(methodDef.LineNumber > 0); // Line where method starts
    }

    [Fact]
    public void ExtractMethodDefinitions_MethodWithParameters_ReturnsMethodDefinitionWithParameters()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public int Add(int a, int b, string name)
        {
            return a + b;
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("Add", methodDef.MethodName);
        Assert.Equal("int", methodDef.ReturnType);
        Assert.Equal(3, methodDef.Parameters.Count);
        Assert.Equal("int", methodDef.Parameters[0]);
        Assert.Equal("int", methodDef.Parameters[1]);
        Assert.Equal("string", methodDef.Parameters[2]);
    }

    [Fact]
    public void ExtractMethodDefinitions_StaticMethod_ReturnsStaticMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static void StaticMethod()
        {
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("StaticMethod", methodDef.MethodName);
        Assert.True(methodDef.IsStatic);
        Assert.Equal("public", methodDef.AccessModifier);
    }

    [Fact]
    public void ExtractMethodDefinitions_VirtualMethod_ReturnsVirtualMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public virtual string VirtualMethod()
        {
            return ""virtual"";
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("VirtualMethod", methodDef.MethodName);
        Assert.True(methodDef.IsVirtual);
        Assert.False(methodDef.IsAbstract);
        Assert.False(methodDef.IsOverride);
    }

    [Fact]
    public void ExtractMethodDefinitions_AbstractMethod_ReturnsAbstractMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public abstract class TestClass
    {
        public abstract string AbstractMethod();
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("AbstractMethod", methodDef.MethodName);
        Assert.True(methodDef.IsAbstract);
        Assert.False(methodDef.IsVirtual);
        Assert.False(methodDef.IsOverride);
    }

    [Fact]
    public void ExtractMethodDefinitions_OverrideMethod_ReturnsOverrideMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public override string ToString()
        {
            return ""test"";
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("ToString", methodDef.MethodName);
        Assert.True(methodDef.IsOverride);
        Assert.False(methodDef.IsAbstract);
    }

    [Fact]
    public void ExtractMethodDefinitions_PrivateMethod_ReturnsPrivateMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        private void PrivateMethod()
        {
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("PrivateMethod", methodDef.MethodName);
        Assert.Equal("private", methodDef.AccessModifier);
    }

    [Fact]
    public void ExtractMethodDefinitions_ProtectedMethod_ReturnsProtectedMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        protected void ProtectedMethod()
        {
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("ProtectedMethod", methodDef.MethodName);
        Assert.Equal("protected", methodDef.AccessModifier);
    }

    [Fact]
    public void ExtractMethodDefinitions_InternalMethod_ReturnsInternalMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        internal void InternalMethod()
        {
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("InternalMethod", methodDef.MethodName);
        Assert.Equal("internal", methodDef.AccessModifier);
    }

    [Fact]
    public void ExtractMethodDefinitions_MethodWithoutAccessModifier_ReturnsPrivateMethodDefinition()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        void MethodWithoutAccessModifier()
        {
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Single(methodDefinitions);
        var methodDef = methodDefinitions[0];
        Assert.Equal("MethodWithoutAccessModifier", methodDef.MethodName);
        Assert.Equal("private", methodDef.AccessModifier); // Default to private
    }

    [Fact]
    public void ExtractMethodDefinitions_MultipleMethods_ReturnsAllMethodDefinitions()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void Method1()
        {
        }

        private static int Method2(string param)
        {
            return 42;
        }

        protected virtual string Method3()
        {
            return ""virtual"";
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
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Equal(3, methodDefinitions.Count);
        
        var method1 = methodDefinitions.First(m => m.MethodName == "Method1");
        Assert.Equal("public", method1.AccessModifier);
        Assert.False(method1.IsStatic);
        Assert.Empty(method1.Parameters);

        var method2 = methodDefinitions.First(m => m.MethodName == "Method2");
        Assert.Equal("private", method2.AccessModifier);
        Assert.True(method2.IsStatic);
        Assert.Equal("int", method2.ReturnType);
        Assert.Single(method2.Parameters);
        Assert.Equal("string", method2.Parameters[0]);

        var method3 = methodDefinitions.First(m => m.MethodName == "Method3");
        Assert.Equal("protected", method3.AccessModifier);
        Assert.True(method3.IsVirtual);
        Assert.Equal("string", method3.ReturnType);
    }

    [Fact]
    public void ExtractMethodDefinitions_EmptyClass_ReturnsEmptyList()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
    }
}";

        var analyzer = new RoslynAnalyzer();
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act
        var methodDefinitions = analyzer.ExtractMethodDefinitions(tree, model);

        // Assert
        Assert.Empty(methodDefinitions);
    }

    [Fact]
    public void ExtractMethodDefinitions_NullTree_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var source = "public class TestClass { }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);
        var model = compilation.GetSemanticModel(tree);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => analyzer.ExtractMethodDefinitions(null!, model));
    }

    [Fact]
    public void ExtractMethodDefinitions_NullModel_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = new RoslynAnalyzer();
        var source = "public class TestClass { }";
        var tree = CSharpSyntaxTree.ParseText(source);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => analyzer.ExtractMethodDefinitions(tree, null!));
    }
}
