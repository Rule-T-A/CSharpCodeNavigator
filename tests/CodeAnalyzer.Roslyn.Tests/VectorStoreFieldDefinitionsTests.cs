using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreFieldDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithFieldDefinitions_StoresFieldDefinitionsInVectorStore()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public string Name;
        private static int Count;
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
            Assert.Equal(2, result.FieldDefinitionCount);
            Assert.Equal(2, result.FieldDefinitions.Count);

            // Verify field definitions were stored in vector store
            var fieldDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "field_definition").ToList();
            Assert.Equal(2, fieldDefWrites.Count);
            
            var fieldDef1 = fieldDefWrites.First(d => d.metadata["field_name"].ToString() == "Name");
            Assert.Equal("field_definition", fieldDef1.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass.Name", fieldDef1.metadata["field"]);
            Assert.Equal("TestClass", fieldDef1.metadata["class"]);
            Assert.Equal("TestNamespace", fieldDef1.metadata["namespace"]);
            Assert.Equal("string", fieldDef1.metadata["field_type"]);
            Assert.Equal("public", fieldDef1.metadata["access_modifier"]);
            Assert.False((bool)fieldDef1.metadata["is_static"]);
            Assert.False((bool)fieldDef1.metadata["is_readonly"]);
            Assert.False((bool)fieldDef1.metadata["is_const"]);

            var fieldDef2 = fieldDefWrites.First(d => d.metadata["field_name"].ToString() == "Count");
            Assert.Equal("field_definition", fieldDef2.metadata["type"]);
            Assert.Equal("TestNamespace.TestClass.Count", fieldDef2.metadata["field"]);
            Assert.Equal("int", fieldDef2.metadata["field_type"]);
            Assert.Equal("private", fieldDef2.metadata["access_modifier"]);
            Assert.True((bool)fieldDef2.metadata["is_static"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithFieldDefinitions_GeneratesCorrectContent()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Name;
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
            var fieldDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "field_definition");
            var content = fieldDefWrite.content;
            
            Assert.Contains("Field", content);
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
    public async Task AnalyzeFileAsync_WithReadOnlyField_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var fieldDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "field_definition");
            Assert.True((bool)fieldDefWrite.metadata["is_readonly"]);
            Assert.False((bool)fieldDefWrite.metadata["is_const"]);
            Assert.False((bool)fieldDefWrite.metadata["is_volatile"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithConstField_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var fieldDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "field_definition");
            Assert.True((bool)fieldDefWrite.metadata["is_const"]);
            Assert.False((bool)fieldDefWrite.metadata["is_readonly"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithVolatileField_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var fieldDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "field_definition");
            Assert.True((bool)fieldDefWrite.metadata["is_volatile"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithMultipleFieldsInOneDeclaration_StoresAllFields()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            Assert.Equal(3, result.FieldDefinitionCount);
            var fieldDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "field_definition").ToList();
            Assert.Equal(3, fieldDefWrites.Count);
            Assert.Contains(fieldDefWrites, f => f.metadata["field_name"].ToString() == "x");
            Assert.Contains(fieldDefWrites, f => f.metadata["field_name"].ToString() == "y");
            Assert.Contains(fieldDefWrites, f => f.metadata["field_name"].ToString() == "z");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithFieldDefinitions_IncludesFileAndLineInfo()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Name;
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
            var fieldDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "field_definition");
            Assert.Equal(tempFile, fieldDefWrite.metadata["file_path"].ToString());
            Assert.True((int)fieldDefWrite.metadata["line_number"] > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

