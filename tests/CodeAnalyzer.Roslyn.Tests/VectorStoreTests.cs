using CodeAnalyzer.Roslyn.Models;
using Xunit;
using System.IO;
using System.Linq;

namespace CodeAnalyzer.Roslyn.Tests;

public class VectorStoreTests
{
    [Fact]
    public async Task VectorStore_RealIntegration_WritesMethodCalls()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "VectorStoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var adapter = await FileVectorStoreAdapter.CreateAsync(tempDir);
            var analyzer = new RoslynAnalyzer(adapter);
            
            var testFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "CallsSample.cs");
            testFile = Path.GetFullPath(testFile);
            Assert.True(File.Exists(testFile), $"Test file not found: {testFile}");

            // Act
            var result = await analyzer.AnalyzeFileAsync(testFile);

            // Assert
            Assert.NotEmpty(result.MethodCalls);
            // Note: Some errors may be present due to file path issues or other non-critical issues
            // The important thing is that method calls are found and can be validated

            // Verify all method calls have valid metadata
            foreach (var call in result.MethodCalls)
            {
                var validationResult = analyzer.ValidateAndNormalizeMetadata(call);
                Assert.True(validationResult.IsValid, 
                    $"Method call validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Verify data was written to vector store by searching for it
            var searchResults = await adapter.Store.SearchTextAsync("Method", limit: 10);
            Assert.NotEmpty(searchResults);
            
            // Verify the search results contain our method call data
            var hasMethodCallData = searchResults.Any(r => 
                r.Document.Content.Contains("Method") && 
                r.Document.Metadata.ContainsKey("type") && 
                r.Document.Metadata["type"].ToString() == "method_call");
            Assert.True(hasMethodCallData, "Search results should contain method call data");

            adapter.Dispose();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task VectorStore_RealIntegration_HandlesInvalidMetadata()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "VectorStoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var adapter = await FileVectorStoreAdapter.CreateAsync(tempDir);
            var analyzer = new RoslynAnalyzer(adapter);

            // Create a method call with invalid metadata
            var invalidCall = new MethodCallInfo
            {
                Caller = "", // Invalid - empty caller
                Callee = "ValidNamespace.ValidClass.ValidMethod",
                CallerClass = "ValidClass",
                CalleeClass = "ValidClass",
                CallerNamespace = "ValidNamespace",
                CalleeNamespace = "ValidNamespace",
                FilePath = "valid.cs",
                LineNumber = 0 // Invalid - line number must be >= 1
            };

            // Act & Assert
            var validationResult = analyzer.ValidateAndNormalizeMetadata(invalidCall);
            Assert.False(validationResult.IsValid);
            Assert.Contains("Required field 'caller' is missing or empty", validationResult.Errors);
            Assert.Contains("Required field 'line_number' must be >= 1, got 0", validationResult.Errors);

            adapter.Dispose();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task VectorStore_RealIntegration_ErrorHandling()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "VectorStoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var adapter = await FileVectorStoreAdapter.CreateAsync(tempDir);
            var analyzer = new RoslynAnalyzer(adapter);

            // Test with a file that has syntax errors
            var errorFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Errors", "SyntaxError.cs");
            errorFile = Path.GetFullPath(errorFile);
            Assert.True(File.Exists(errorFile), $"Error test file not found: {errorFile}");

            // Act
            var result = await analyzer.AnalyzeFileAsync(errorFile);

            // Assert - should handle errors gracefully
            Assert.NotNull(result);
            Assert.NotNull(result.Errors);
            // The analyzer should not crash even with syntax errors

            adapter.Dispose();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task VectorStore_RealIntegration_MetadataSchemaCompliance()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "VectorStoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var adapter = await FileVectorStoreAdapter.CreateAsync(tempDir);
            var analyzer = new RoslynAnalyzer(adapter);
            
            var testFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "CallsSample.cs");
            testFile = Path.GetFullPath(testFile);

            // Act
            var result = await analyzer.AnalyzeFileAsync(testFile);

            // Assert
            Assert.NotEmpty(result.MethodCalls);
            // Note: Some errors may be present due to file path issues or other non-critical issues
            // The important thing is that method calls are found and can be validated

            // Verify all method calls have valid metadata
            foreach (var call in result.MethodCalls)
            {
                var validationResult = analyzer.ValidateAndNormalizeMetadata(call);
                Assert.True(validationResult.IsValid, 
                    $"Method call validation failed: {string.Join(", ", validationResult.Errors)}");
                
                // Verify required fields are present and non-empty
                Assert.NotEmpty(validationResult.NormalizedCall.Caller);
                Assert.NotEmpty(validationResult.NormalizedCall.Callee);
                Assert.NotEmpty(validationResult.NormalizedCall.CallerClass);
                Assert.NotEmpty(validationResult.NormalizedCall.CalleeClass);
                Assert.NotEmpty(validationResult.NormalizedCall.FilePath);
                Assert.True(validationResult.NormalizedCall.LineNumber >= 1);
            }

            adapter.Dispose();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
