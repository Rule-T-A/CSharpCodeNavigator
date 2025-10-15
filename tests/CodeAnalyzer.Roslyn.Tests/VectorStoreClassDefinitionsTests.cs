using CodeAnalyzer.Roslyn.Models;
using Xunit;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreClassDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithClassDefinitions_StoresClassDefinitionsInVectorStore()
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

    public static class StaticClass
    {
        public static void StaticMethod()
        {
        }
    }
}";
        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(2, result.ClassDefinitionCount);
            Assert.Equal(2, result.ClassDefinitions.Count);

            // Verify class definitions were stored in vector store
            var classDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "class_definition").ToList();
            Assert.Equal(2, classDefWrites.Count);
            
            var testClassDef = classDefWrites.First(d => d.metadata["class_name"].ToString() == "TestClass");
            Assert.Equal("class_definition", testClassDef.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass", testClassDef.metadata["class"]);
            Assert.Equal("TestClass", testClassDef.metadata["class_name"]);
            Assert.Equal("TestNamespace", testClassDef.metadata["namespace"]);
            Assert.Equal("public", testClassDef.metadata["access_modifier"]);
            Assert.False((bool)testClassDef.metadata["is_static"]);
            Assert.False((bool)testClassDef.metadata["is_abstract"]);
            Assert.False((bool)testClassDef.metadata["is_sealed"]);

            var staticClassDef = classDefWrites.First(d => d.metadata["class_name"].ToString() == "StaticClass");
            Assert.Equal("class_definition", staticClassDef.metadata["type"]);
            Assert.Equal("TestNamespace.StaticClass", staticClassDef.metadata["class"]);
            Assert.Equal("StaticClass", staticClassDef.metadata["class_name"]);
            Assert.Equal("TestNamespace", staticClassDef.metadata["namespace"]);
            Assert.Equal("public", staticClassDef.metadata["access_modifier"]);
            Assert.True((bool)staticClassDef.metadata["is_static"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithInheritance_StoresInheritanceInfo()
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
        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(2, result.ClassDefinitionCount);

            // Verify inheritance information is stored
            var classDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "class_definition").ToList();
            Assert.NotEmpty(classDefWrites);
            var derivedClassDef = classDefWrites.First(d => d.metadata["class_name"].ToString() == "DerivedClass");
            Assert.Equal("TestNamespace.BaseClass", derivedClassDef.metadata["base_class"]);
            Assert.Equal("TestNamespace.IInterface", derivedClassDef.metadata["interfaces"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithAbstractClass_StoresAbstractInfo()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public abstract class AbstractClass
    {
        public abstract void AbstractMethod();
        
        public void ConcreteMethod()
        {
        }
    }
}";
        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Single(result.ClassDefinitions);

            var classDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "class_definition");
            Assert.Equal("class_definition", classDef.metadata["type"]);
            Assert.Equal("AbstractClass", classDef.metadata["class_name"]);
            Assert.True((bool)classDef.metadata["is_abstract"]);
            Assert.False((bool)classDef.metadata["is_static"]);
            Assert.False((bool)classDef.metadata["is_sealed"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithSealedClass_StoresSealedInfo()
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
        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Single(result.ClassDefinitions);

            var classDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "class_definition");
            Assert.Equal("class_definition", classDef.metadata["type"]);
            Assert.Equal("SealedClass", classDef.metadata["class_name"]);
            Assert.True((bool)classDef.metadata["is_sealed"]);
            Assert.False((bool)classDef.metadata["is_abstract"]);
            Assert.False((bool)classDef.metadata["is_static"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithComplexClass_StoresAllClassProperties()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public abstract class ComplexClass : IDisposable
    {
        private string _field;
        
        public string Property { get; set; }
        
        public void Method1()
        {
        }
        
        public void Method2()
        {
        }
        
        public abstract void AbstractMethod();
        
        public void Dispose()
        {
        }
    }
}";
        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Single(result.ClassDefinitions);

            var classDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "class_definition");
            Assert.Equal("class_definition", classDef.metadata["type"]);
            Assert.Equal("ComplexClass", classDef.metadata["class_name"]);
            Assert.Equal("TestNamespace", classDef.metadata["namespace"]);
            Assert.Equal("public", classDef.metadata["access_modifier"]);
            Assert.True((bool)classDef.metadata["is_abstract"]);
            Assert.False((bool)classDef.metadata["is_static"]);
            Assert.False((bool)classDef.metadata["is_sealed"]);
            Assert.Equal("System.IDisposable", classDef.metadata["interfaces"]);
            Assert.Equal(4, (int)classDef.metadata["method_count"]);
            Assert.Equal(1, (int)classDef.metadata["property_count"]);
            Assert.Equal(1, (int)classDef.metadata["field_count"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_ClassDefinitionContent_ContainsCorrectDescription()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void Method()
        {
        }
    }
}";
        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.True(result.IsSuccessful);

            var classDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "class_definition");
            var content = classDef.content;
            
            Assert.Contains("Class TestClass defined in namespace TestNamespace", content);
            Assert.Contains("This is a public class", content);
            Assert.Contains("methods", content);
            Assert.Contains("properties", content);
            Assert.Contains("fields", content);
            Assert.Contains("Defined in file", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
