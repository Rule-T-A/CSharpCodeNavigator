using CodeAnalyzer.Roslyn.Models;
using Xunit;

namespace CodeAnalyzer.Roslyn.Tests;

public class MetadataValidationTests
{
    private readonly RoslynAnalyzer _analyzer = new();

    [Fact]
    public void ValidateAndNormalizeMetadata_ValidData_ReturnsValidResult()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "C:\\path\\to\\file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("Namespace.Class.Method", result.NormalizedCall.Caller);
        Assert.Equal("OtherNamespace.OtherClass.OtherMethod", result.NormalizedCall.Callee);
        Assert.Equal("Class", result.NormalizedCall.CallerClass);
        Assert.Equal("OtherClass", result.NormalizedCall.CalleeClass);
        Assert.Equal("Namespace", result.NormalizedCall.CallerNamespace);
        Assert.Equal("OtherNamespace", result.NormalizedCall.CalleeNamespace);
        Assert.Equal("C:\\path\\to\\file.cs", result.NormalizedCall.FilePath);
        Assert.Equal(42, result.NormalizedCall.LineNumber);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_TrimsWhitespace()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "  Namespace.Class.Method  ",
            Callee = "\tOtherNamespace.OtherClass.OtherMethod\t",
            CallerClass = " Class ",
            CalleeClass = " OtherClass ",
            CallerNamespace = " Namespace ",
            CalleeNamespace = " OtherNamespace ",
            FilePath = "  C:\\path\\to\\file.cs  ",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("Namespace.Class.Method", result.NormalizedCall.Caller);
        Assert.Equal("OtherNamespace.OtherClass.OtherMethod", result.NormalizedCall.Callee);
        Assert.Equal("Class", result.NormalizedCall.CallerClass);
        Assert.Equal("OtherClass", result.NormalizedCall.CalleeClass);
        Assert.Equal("Namespace", result.NormalizedCall.CallerNamespace);
        Assert.Equal("OtherNamespace", result.NormalizedCall.CalleeNamespace);
        Assert.Equal("C:\\path\\to\\file.cs", result.NormalizedCall.FilePath);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_MissingCaller_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = null,
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'caller' is missing or empty", result.Errors);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_EmptyCaller_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "   ",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'caller' is missing or empty", result.Errors);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_MissingCallee_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'callee' is missing or empty", result.Errors);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_MissingCallerClass_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = null,
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'caller_class' is missing or empty", result.Errors);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_MissingCalleeClass_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'callee_class' is missing or empty", result.Errors);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_MissingFilePath_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = null,
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'file_path' is missing or empty", result.Errors);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_InvalidLineNumber_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 0
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'line_number' must be >= 1, got 0", result.Errors);
        Assert.Equal(1, result.NormalizedCall.LineNumber); // Should default to 1
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_NegativeLineNumber_ReturnsError()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "Namespace.Class.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "Class",
            CalleeClass = "OtherClass",
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = -5
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'line_number' must be >= 1, got -5", result.Errors);
        Assert.Equal(1, result.NormalizedCall.LineNumber); // Should default to 1
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_EmptyNamespace_Allowed()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = "GlobalClass.Method",
            Callee = "OtherNamespace.OtherClass.OtherMethod",
            CallerClass = "GlobalClass",
            CalleeClass = "OtherClass",
            CallerNamespace = "", // Empty namespace (global)
            CalleeNamespace = "OtherNamespace",
            FilePath = "file.cs",
            LineNumber = 42
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("", result.NormalizedCall.CallerNamespace);
    }

    [Fact]
    public void ValidateAndNormalizeMetadata_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var call = new MethodCallInfo
        {
            Caller = null,
            Callee = "",
            CallerClass = "   ",
            CalleeClass = null,
            CallerNamespace = "Namespace",
            CalleeNamespace = "OtherNamespace",
            FilePath = "",
            LineNumber = 0
        };

        // Act
        var result = _analyzer.ValidateAndNormalizeMetadata(call);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Required field 'caller' is missing or empty", result.Errors);
        Assert.Contains("Required field 'callee' is missing or empty", result.Errors);
        Assert.Contains("Required field 'caller_class' is missing or empty", result.Errors);
        Assert.Contains("Required field 'callee_class' is missing or empty", result.Errors);
        Assert.Contains("Required field 'file_path' is missing or empty", result.Errors);
        Assert.Contains("Required field 'line_number' must be >= 1, got 0", result.Errors);
        Assert.Equal(6, result.Errors.Count);
    }
}
