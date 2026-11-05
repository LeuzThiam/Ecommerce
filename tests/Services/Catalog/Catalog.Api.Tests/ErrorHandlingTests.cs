using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public class ErrorHandlingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateCategory_WithEmptyName_ReturnsProblemDetailsNotGenericBody()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken("Administrator"));

        var response = await client.PostAsJsonAsync("/api/catalog/categories", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.Equal("Le nom de la catégorie est requis.", problem.Detail);
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
