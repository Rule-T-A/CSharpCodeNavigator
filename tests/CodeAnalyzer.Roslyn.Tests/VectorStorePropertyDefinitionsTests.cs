using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStorePropertyDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithPropertyDefinitions_StoresPropertyDefinitionsInVectorStore()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public string Name { get; set; }
        private static int Count { get; }
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
            Assert.Equal(2, result.PropertyDefinitionCount);
            Assert.Equal(2, result.PropertyDefinitions.Count);

            // Verify property definitions were stored in vector store
            var propertyDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "property_definition").ToList();
            Assert.Equal(2, propertyDefWrites.Count);
            
            var propertyDef1 = propertyDefWrites.First(d => d.metadata["property_name"].ToString() == "Name");
            Assert.Equal("property_definition", propertyDef1.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass.Name", propertyDef1.metadata["property"]);
            Assert.Equal("TestClass", propertyDef1.metadata["class"]);
            Assert.Equal("TestNamespace", propertyDef1.metadata["namespace"]);
            Assert.Equal("string", propertyDef1.metadata["property_type"]);
            Assert.Equal("public", propertyDef1.metadata["access_modifier"]);
            Assert.False((bool)propertyDef1.metadata["is_static"]);
            Assert.True((bool)propertyDef1.metadata["has_getter"]);
            Assert.True((bool)propertyDef1.metadata["has_setter"]);

            var propertyDef2 = propertyDefWrites.First(d => d.metadata["property_name"].ToString() == "Count");
            Assert.Equal("property_definition", propertyDef2.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass.Count", propertyDef2.metadata["property"]);
            Assert.Equal("int", propertyDef2.metadata["property_type"]);
            Assert.Equal("private", propertyDef2.metadata["access_modifier"]);
            Assert.True((bool)propertyDef2.metadata["is_static"]);
            Assert.True((bool)propertyDef2.metadata["has_getter"]);
            Assert.False((bool)propertyDef2.metadata["has_setter"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithPropertyDefinitions_GeneratesCorrectContent()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Name { get; set; }
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
            var propertyDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "property_definition");
            var content = propertyDefWrite.content;
            
            Assert.Contains("Property", content);
            Assert.Contains("Name", content);
            Assert.Contains("TestClass", content);
            Assert.Contains("TestNamespace", content);
            Assert.Contains("string", content);
            Assert.Contains("public", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithVirtualProperty_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var propertyDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "property_definition");
            Assert.True((bool)propertyDefWrite.metadata["is_virtual"]);
            Assert.False((bool)propertyDefWrite.metadata["is_abstract"]);
            Assert.False((bool)propertyDefWrite.metadata["is_override"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithAbstractProperty_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var propertyDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "property_definition");
            Assert.True((bool)propertyDefWrite.metadata["is_abstract"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithPropertyWithAccessors_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var propertyDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "property_definition");
            Assert.True((bool)propertyDefWrite.metadata["has_getter"]);
            Assert.True((bool)propertyDefWrite.metadata["has_setter"]);
            Assert.False((bool)propertyDefWrite.metadata["is_auto_property"]); // Has accessor bodies
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithPropertyDefinitions_IncludesFileAndLineInfo()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Name { get; set; }
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
            var propertyDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "property_definition");
            Assert.Equal(tempFile, propertyDefWrite.metadata["file_path"].ToString());
            Assert.True((int)propertyDefWrite.metadata["line_number"] > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

