using Microsoft.AspNetCore.Mvc.Testing;
using CodeAnalyzer.Api;

namespace CodeAnalyzer.Api.Tests;

/// <summary>
/// Tests to verify the API project setup and basic functionality.
/// </summary>
public class ProjectSetupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectSetupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Project_Builds_Successfully()
    {
        // This test verifies that the project compiles without errors
        Assert.NotNull(_factory);
    }

    [Fact]
    public async Task Server_Starts_And_Responds()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        // The root endpoint may not exist yet, but we're verifying the server starts
        // and can handle requests (even if it returns 404)
        Assert.NotNull(response);
    }
}

