using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class EnumValueInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var enumValue = new EnumValueInfo();

        // Assert
        Assert.Equal(string.Empty, enumValue.ValueName);
        Assert.Null(enumValue.Value);
        Assert.Equal(0, enumValue.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Act
        var enumValue = new EnumValueInfo("Value1", 42, 10);

        // Assert
        Assert.Equal("Value1", enumValue.ValueName);
        Assert.Equal(42, enumValue.Value);
        Assert.Equal(10, enumValue.LineNumber);
    }

    [Fact]
    public void ToString_WithValue_ReturnsFormattedString()
    {
        // Arrange
        var enumValue = new EnumValueInfo("Value1", 42, 10);

        // Act
        var result = enumValue.ToString();

        // Assert
        Assert.Equal("Value1 = 42", result);
    }

    [Fact]
    public void ToString_WithoutValue_ReturnsValueName()
    {
        // Arrange
        var enumValue = new EnumValueInfo("Value1", null, 10);

        // Act
        var result = enumValue.ToString();

        // Assert
        Assert.Equal("Value1", result);
    }

    [Fact]
    public void ToString_WithStringValue_ReturnsFormattedString()
    {
        // Arrange
        var enumValue = new EnumValueInfo("Value1", "Test", 10);

        // Act
        var result = enumValue.ToString();

        // Assert
        Assert.Equal("Value1 = Test", result);
    }
}

