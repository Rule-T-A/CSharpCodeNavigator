using CodeAnalyzer.Roslyn.Models;

namespace CodeAnalyzer.Roslyn.Tests.Models;

public class MethodCallInfoTests
{
    [Fact]
    public void Constructor_Default_InitializesProperties()
    {
        // Arrange & Act
        var methodCall = new MethodCallInfo();

        // Assert
        Assert.Equal(string.Empty, methodCall.Caller);
        Assert.Equal(string.Empty, methodCall.Callee);
        Assert.Equal(string.Empty, methodCall.CallerClass);
        Assert.Equal(string.Empty, methodCall.CalleeClass);
        Assert.Equal(string.Empty, methodCall.CallerNamespace);
        Assert.Equal(string.Empty, methodCall.CalleeNamespace);
        Assert.Equal(string.Empty, methodCall.FilePath);
        Assert.Equal(0, methodCall.LineNumber);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var caller = "MyApp.Controllers.LoginController.Login";
        var callee = "MyApp.Services.UserService.ValidateUser";
        var callerClass = "LoginController";
        var calleeClass = "UserService";
        var callerNamespace = "MyApp.Controllers";
        var calleeNamespace = "MyApp.Services";
        var filePath = "Controllers/LoginController.cs";
        var lineNumber = 42;

        // Act
        var methodCall = new MethodCallInfo(
            caller, callee, callerClass, calleeClass,
            callerNamespace, calleeNamespace, filePath, lineNumber);

        // Assert
        Assert.Equal(caller, methodCall.Caller);
        Assert.Equal(callee, methodCall.Callee);
        Assert.Equal(callerClass, methodCall.CallerClass);
        Assert.Equal(calleeClass, methodCall.CalleeClass);
        Assert.Equal(callerNamespace, methodCall.CallerNamespace);
        Assert.Equal(calleeNamespace, methodCall.CalleeNamespace);
        Assert.Equal(filePath, methodCall.FilePath);
        Assert.Equal(lineNumber, methodCall.LineNumber);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var methodCall = new MethodCallInfo(
            "MyApp.Controllers.LoginController.Login",
            "MyApp.Services.UserService.ValidateUser",
            "LoginController",
            "UserService",
            "MyApp.Controllers",
            "MyApp.Services",
            "Controllers/LoginController.cs",
            42);

        // Act
        var result = methodCall.ToString();

        // Assert
        Assert.Equal("MyApp.Controllers.LoginController.Login -> MyApp.Services.UserService.ValidateUser (line 42 in Controllers/LoginController.cs)", result);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var methodCall = new MethodCallInfo();

        // Act
        methodCall.Caller = "Test.Caller";
        methodCall.Callee = "Test.Callee";
        methodCall.CallerClass = "CallerClass";
        methodCall.CalleeClass = "CalleeClass";
        methodCall.CallerNamespace = "Test.CallerNamespace";
        methodCall.CalleeNamespace = "Test.CalleeNamespace";
        methodCall.FilePath = "test.cs";
        methodCall.LineNumber = 123;

        // Assert
        Assert.Equal("Test.Caller", methodCall.Caller);
        Assert.Equal("Test.Callee", methodCall.Callee);
        Assert.Equal("CallerClass", methodCall.CallerClass);
        Assert.Equal("CalleeClass", methodCall.CalleeClass);
        Assert.Equal("Test.CallerNamespace", methodCall.CallerNamespace);
        Assert.Equal("Test.CalleeNamespace", methodCall.CalleeNamespace);
        Assert.Equal("test.cs", methodCall.FilePath);
        Assert.Equal(123, methodCall.LineNumber);
    }
}
