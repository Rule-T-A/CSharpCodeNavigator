using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreMethodDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithMethodDefinitions_StoresMethodDefinitionsInVectorStore()
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

        private static int Add(int a, int b)
        {
            return a + b;
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
            Assert.Equal(2, result.MethodDefinitionCount);
            Assert.Equal(2, result.MethodDefinitions.Count);

            // Verify method definitions were stored in vector store
            var methodDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "method_definition").ToList();
            Assert.Equal(2, methodDefWrites.Count);
            
            var methodDef1 = fakeWriter.Writes.First(d => d.metadata["method_name"].ToString() == "GetName");
            Assert.Equal("method_definition", methodDef1.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass.GetName", methodDef1.metadata["method"]);
            Assert.Equal("TestClass", methodDef1.metadata["class"]);
            Assert.Equal("TestNamespace", methodDef1.metadata["namespace"]);
            Assert.Equal("string", methodDef1.metadata["return_type"]);
            Assert.Equal("public", methodDef1.metadata["access_modifier"]);
            Assert.False((bool)methodDef1.metadata["is_static"]);

            var methodDef2 = fakeWriter.Writes.First(d => d.metadata["method_name"].ToString() == "Add");
            Assert.Equal("method_definition", methodDef2.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass.Add", methodDef2.metadata["method"]);
            Assert.Equal("TestClass", methodDef2.metadata["class"]);
            Assert.Equal("TestNamespace", methodDef2.metadata["namespace"]);
            Assert.Equal("int", methodDef2.metadata["return_type"]);
            Assert.Equal("private", methodDef2.metadata["access_modifier"]);
            Assert.True((bool)methodDef2.metadata["is_static"]);
            Assert.Equal("int, int", methodDef2.metadata["parameters"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithMethodDefinitions_GeneratesCorrectContent()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public virtual string GetName()
        {
            return ""test"";
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
            await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var methodDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "method_definition");
            var content = methodDef.content;
            
            Assert.Contains("Method GetName", content);
            Assert.Contains("class TestClass", content);
            Assert.Contains("namespace TestNamespace", content);
            Assert.Contains("returns string", content);
            Assert.Contains("Access modifier: public", content);
            Assert.Contains("Modifiers: virtual", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithAbstractMethod_StoresAbstractMethodDefinition()
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
            Assert.Equal(1, result.MethodDefinitionCount);

            var methodDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "method_definition");
            Assert.Equal("AbstractMethod", methodDef.metadata["method_name"]);
            Assert.True((bool)methodDef.metadata["is_abstract"]);
            Assert.False((bool)methodDef.metadata["is_virtual"]);
            Assert.False((bool)methodDef.metadata["is_override"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithOverrideMethod_StoresOverrideMethodDefinition()
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
            Assert.Equal(1, result.MethodDefinitionCount);

            var methodDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "method_definition");
            Assert.Equal("ToString", methodDef.metadata["method_name"]);
            Assert.True((bool)methodDef.metadata["is_override"]);
            Assert.False((bool)methodDef.metadata["is_abstract"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithComplexMethod_StoresAllMethodProperties()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public static string ComplexMethod(int param1, string param2, bool param3)
        {
            return ""complex"";
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
            Assert.Equal(1, result.MethodDefinitionCount);

            var methodDef = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "method_definition");
            Assert.Equal("ComplexMethod", methodDef.metadata["method_name"]);
            Assert.Equal("TestNamespace.TestClass.ComplexMethod", methodDef.metadata["method"]);
            Assert.Equal("TestClass", methodDef.metadata["class"]);
            Assert.Equal("TestNamespace", methodDef.metadata["namespace"]);
            Assert.Equal("string", methodDef.metadata["return_type"]);
            Assert.Equal("public", methodDef.metadata["access_modifier"]);
            Assert.True((bool)methodDef.metadata["is_static"]);
            Assert.False((bool)methodDef.metadata["is_virtual"]);
            Assert.False((bool)methodDef.metadata["is_abstract"]);
            Assert.False((bool)methodDef.metadata["is_override"]);
            Assert.Equal("int, string, bool", methodDef.metadata["parameters"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
