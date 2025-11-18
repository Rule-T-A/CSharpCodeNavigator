using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreStructDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithStructDefinitions_StoresStructDefinitionsInVectorStore()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct TestStruct
    {
        public int Field1;
        public string Property1 { get; set; }
        public void Method1() { }
    }

    internal struct InternalStruct
    {
        public int Field2;
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
            Assert.Equal(2, result.StructDefinitionCount);
            Assert.Equal(2, result.StructDefinitions.Count);

            // Verify struct definitions were stored in vector store
            var structDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "struct_definition").ToList();
            Assert.Equal(2, structDefWrites.Count);
            
            var structDef1 = structDefWrites.First(d => d.metadata["struct_name"].ToString() == "TestStruct");
            Assert.Equal("struct_definition", structDef1.metadata["type"]);
            Assert.Equal("TestNamespace.TestStruct", structDef1.metadata["struct"]);
            Assert.Equal("TestNamespace", structDef1.metadata["namespace"]);
            Assert.Equal("public", structDef1.metadata["access_modifier"]);
            Assert.Equal(1, (int)structDef1.metadata["method_count"]);
            Assert.Equal(1, (int)structDef1.metadata["property_count"]);
            Assert.Equal(1, (int)structDef1.metadata["field_count"]);
            Assert.False((bool)structDef1.metadata["is_readonly"]);
            Assert.False((bool)structDef1.metadata["is_ref"]);

            var structDef2 = structDefWrites.First(d => d.metadata["struct_name"].ToString() == "InternalStruct");
            Assert.Equal("struct_definition", structDef2.metadata["type"]);
            Assert.Equal("internal", structDef2.metadata["access_modifier"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithStructDefinitions_GeneratesCorrectContent()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct TestStruct
    {
        public int Field1;
        public string Property1 { get; set; }
        public void Method1() { }
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
            var structDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "struct_definition");
            var content = structDefWrite.content;
            
            Assert.Contains("Struct", content);
            Assert.Contains("TestStruct", content);
            Assert.Contains("TestNamespace", content);
            Assert.Contains("public", content);
            Assert.Contains("method", content);
            Assert.Contains("property", content);
            Assert.Contains("field", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithReadOnlyStruct_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public readonly struct TestStruct
    {
        public int Field1;
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
            var structDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "struct_definition");
            Assert.True((bool)structDefWrite.metadata["is_readonly"]);
            Assert.False((bool)structDefWrite.metadata["is_ref"]);
            var content = structDefWrite.content;
            Assert.Contains("readonly", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithRefStruct_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public ref struct TestStruct
    {
        public int Field1;
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
            var structDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "struct_definition");
            Assert.False((bool)structDefWrite.metadata["is_readonly"]);
            Assert.True((bool)structDefWrite.metadata["is_ref"]);
            var content = structDefWrite.content;
            Assert.Contains("ref", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithStructWithInterfaces_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IDisposable { }
    public interface IComparable { }
    
    public struct TestStruct : IDisposable, IComparable
    {
        public void Method1() { }
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
            var structDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "struct_definition" && d.metadata["struct_name"].ToString() == "TestStruct");
            var interfaces = structDefWrite.metadata["interfaces"].ToString();
            Assert.Contains("IDisposable", interfaces);
            Assert.Contains("IComparable", interfaces);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithStructDefinitions_IncludesFileAndLineInfo()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct TestStruct
    {
        public int Field1;
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
            var structDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "struct_definition");
            Assert.Equal(tempFile, structDefWrite.metadata["file_path"].ToString());
            Assert.True((int)structDefWrite.metadata["line_number"] > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEmptyStruct_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public struct EmptyStruct
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
            var structDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "struct_definition");
            Assert.Equal(0, (int)structDefWrite.metadata["method_count"]);
            Assert.Equal(0, (int)structDefWrite.metadata["property_count"]);
            Assert.Equal(0, (int)structDefWrite.metadata["field_count"]);
            Assert.Equal("EmptyStruct", structDefWrite.metadata["struct_name"].ToString());
            Assert.Equal("none", structDefWrite.metadata["interfaces"].ToString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

