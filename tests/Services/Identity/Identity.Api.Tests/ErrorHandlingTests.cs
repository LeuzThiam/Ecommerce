using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Tests;

public class ErrorHandlingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsProblemDetailsNotGenericBody()
    {
        var response = await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "pas-un-email",
            password = "MotDePasse123!",
            displayName = "Test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
        Assert.Equal("L'adresse email n'est pas valide.", problem.Detail);
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
