using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public class AuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/catalog/categories", new { name = "Sans token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithClientRole_ReturnsForbidden()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken("Client"));

        var response = await client.PostAsJsonAsync("/api/catalog/categories", new { name = "Tentative client" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithAdministratorRole_ReturnsCreated()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken("Administrator"));

        var response = await client.PostAsJsonAsync("/api/catalog/categories", new { name = $"Categorie-{Guid.NewGuid()}" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
