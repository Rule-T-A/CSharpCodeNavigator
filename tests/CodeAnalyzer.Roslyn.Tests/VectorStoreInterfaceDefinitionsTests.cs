using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;
using CodeAnalyzer.Roslyn.Tests;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreInterfaceDefinitionsTests
{
    [Fact]
    public async Task AnalyzeFileAsync_WithInterfaceDefinitions_StoresInterfaceDefinitionsInVectorStore()
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

    internal interface IInternalInterface
    {
        void Method2();
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
            Assert.Equal(2, result.InterfaceDefinitionCount);
            Assert.Equal(2, result.InterfaceDefinitions.Count);

            // Verify interface definitions were stored in vector store
            var interfaceDefWrites = fakeWriter.Writes.Where(d => d.metadata["type"].ToString() == "interface_definition").ToList();
            Assert.Equal(2, interfaceDefWrites.Count);
            
            var interfaceDef1 = interfaceDefWrites.First(d => d.metadata["interface_name"].ToString() == "ITestInterface");
            Assert.Equal("interface_definition", interfaceDef1.metadata["type"]);
            Assert.Equal("TestNamespace.ITestInterface", interfaceDef1.metadata["interface"]);
            Assert.Equal("TestNamespace", interfaceDef1.metadata["namespace"]);
            Assert.Equal("public", interfaceDef1.metadata["access_modifier"]);
            Assert.Equal(1, (int)interfaceDef1.metadata["method_count"]);
            Assert.Equal(1, (int)interfaceDef1.metadata["property_count"]);

            var interfaceDef2 = interfaceDefWrites.First(d => d.metadata["interface_name"].ToString() == "IInternalInterface");
            Assert.Equal("interface_definition", interfaceDef2.metadata["type"]);
            Assert.Equal("internal", interfaceDef2.metadata["access_modifier"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithInterfaceDefinitions_GeneratesCorrectContent()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var interfaceDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "interface_definition");
            var content = interfaceDefWrite.content;
            
            Assert.Contains("Interface", content);
            Assert.Contains("ITestInterface", content);
            Assert.Contains("TestNamespace", content);
            Assert.Contains("public", content);
            Assert.Contains("method", content);
            Assert.Contains("property", content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithInterfaceWithBaseInterfaces_StoresCorrectMetadata()
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

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, source);

        var fakeWriter = new FakeVectorStoreWriter();
        var analyzer = new RoslynAnalyzer(fakeWriter);

        try
        {
            // Act
            var result = await analyzer.AnalyzeFileAsync(tempFile);

            // Assert
            var interfaceDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "interface_definition" && d.metadata["interface_name"].ToString() == "ITestInterface");
            var baseInterfaces = interfaceDefWrite.metadata["base_interfaces"].ToString();
            Assert.Contains("IBaseInterface1", baseInterfaces);
            Assert.Contains("IBaseInterface2", baseInterfaces);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithInterfaceDefinitions_IncludesFileAndLineInfo()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface ITestInterface
    {
        void Method1();
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
            var interfaceDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "interface_definition");
            Assert.Equal(tempFile, interfaceDefWrite.metadata["file_path"].ToString());
            Assert.True((int)interfaceDefWrite.metadata["line_number"] > 0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEmptyInterface_StoresCorrectMetadata()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IEmptyInterface
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
            var interfaceDefWrite = fakeWriter.Writes.First(d => d.metadata["type"].ToString() == "interface_definition");
            Assert.Equal(0, (int)interfaceDefWrite.metadata["method_count"]);
            Assert.Equal(0, (int)interfaceDefWrite.metadata["property_count"]);
            Assert.Equal("IEmptyInterface", interfaceDefWrite.metadata["interface_name"].ToString());
            Assert.Equal("none", interfaceDefWrite.metadata["base_interfaces"].ToString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

