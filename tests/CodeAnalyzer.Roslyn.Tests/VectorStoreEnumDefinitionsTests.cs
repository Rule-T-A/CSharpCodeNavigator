using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreEnumDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithEnumDefinitions_StoresEnumDefinitionsInVectorStore()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public enum TestEnum
    {
        Value1,
        Value2
    }

    internal enum InternalEnum : byte
    {
        Value3 = 10
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
            Assert.Equal(2, result.EnumDefinitionCount);
            Assert.Equal(2, result.EnumDefinitions.Count);

            // Verify enum definitions were stored in vector store
            var enumDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "enum_definition").ToList();
            Assert.Equal(2, enumDefWrites.Count);
            
            var enumDef1 = enumDefWrites.First(d => d.metadata["enum_name"].ToString() == "TestEnum");
            Assert.Equal("enum_definition", enumDef1.metadata["type"]);
            Assert.Equal("TestNamespace.TestEnum", enumDef1.metadata["enum"]);
            Assert.Equal("TestNamespace", enumDef1.metadata["namespace"]);
            Assert.Equal("public", enumDef1.metadata["access_modifier"]);
            Assert.Equal("int", enumDef1.metadata["underlying_type"]);
            Assert.Equal(2, (int)enumDef1.metadata["value_count"]);

            var enumDef2 = enumDefWrites.First(d => d.metadata["enum_name"].ToString() == "InternalEnum");
            Assert.Equal("enum_definition", enumDef2.metadata["type"]);
            Assert.Equal("TestNamespace.InternalEnum", enumDef2.metadata["enum"]);
            Assert.Equal("byte", enumDef2.metadata["underlying_type"]);
            Assert.Equal("internal", enumDef2.metadata["access_modifier"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEnumDefinitions_GeneratesCorrectContent()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum
    {
        Value1,
        Value2
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
            var enumDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "enum_definition");
            var content = enumDefWrite.content;
            
            Assert.Contains("Enum", content);
            Assert.Contains("TestEnum", content);
            Assert.Contains("TestNamespace", content);
            Assert.Contains("int", content);
            Assert.Contains("Value1", content);
            Assert.Contains("Value2", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEnumWithExplicitValues_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum
    {
        Value1 = 10,
        Value2 = 20
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
            var enumDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "enum_definition");
            Assert.Equal(2, (int)enumDefWrite.metadata["value_count"]);
            var valuesStr = enumDefWrite.metadata["values"].ToString();
            Assert.Contains("Value1 = 10", valuesStr);
            Assert.Contains("Value2 = 20", valuesStr);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEnumWithByteType_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum : byte
    {
        Value1
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
            var enumDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "enum_definition");
            Assert.Equal("byte", enumDefWrite.metadata["underlying_type"].ToString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEnumDefinitions_IncludesFileAndLineInfo()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum TestEnum
    {
        Value1
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
            var enumDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "enum_definition");
            Assert.Equal(tempFile, enumDefWrite.metadata["file_path"].ToString());
            Assert.True((int)enumDefWrite.metadata["line_number"] > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEmptyEnum_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public enum EmptyEnum
    {
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
            var enumDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "enum_definition");
            Assert.Equal(0, (int)enumDefWrite.metadata["value_count"]);
            Assert.Equal("EmptyEnum", enumDefWrite.metadata["enum_name"].ToString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

