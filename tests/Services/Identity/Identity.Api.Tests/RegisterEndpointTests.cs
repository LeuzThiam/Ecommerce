using System.Net;
using System.Net.Http.Json;

namespace Identity.Api.Tests;

public class RegisterEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "test1@example.com",
            password = "MotDePasse123!",
            displayName = "Test Un"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var request = new
        {
            email = "test2@example.com",
            password = "MotDePasse123!",
            displayName = "Test Deux"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/identity/register", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/api/identity/register", request);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "test3@example.com",
            password = "123",
            displayName = "Test Trois"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
