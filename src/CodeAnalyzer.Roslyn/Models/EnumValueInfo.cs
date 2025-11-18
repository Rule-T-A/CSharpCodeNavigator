namespace CodeAnalyzer.Roslyn.Models;

/// <summary>
/// Represents an enum value within an enum definition.
/// </summary>
public class EnumValueInfo
{
    /// <summary>
    /// Name of the enum value
    /// </summary>
    public string ValueName { get; set; } = string.Empty;

    /// <summary>
    /// Actual value of the enum (can be int, byte, etc.)
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Line number where the enum value is defined (1-based)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Creates a new instance of EnumValueInfo
    /// </summary>
    public EnumValueInfo()
    {
    }

    /// <summary>
    /// Creates a new instance of EnumValueInfo with the specified values
    /// </summary>
    public EnumValueInfo(string valueName, object? value, int lineNumber)
    {
        ValueName = valueName;
        Value = value;
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Returns a string representation of the enum value
    /// </summary>
    public override string ToString()
    {
        if (Value != null)
        {
            return $"{ValueName} = {Value}";
        }
        return ValueName;
    }
}

