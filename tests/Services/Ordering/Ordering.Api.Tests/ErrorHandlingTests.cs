using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ordering.Api.Tests;

public class ErrorHandlingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Checkout_WithEmptyCart_ReturnsProblemDetailsNotGenericBody()
    {
        var customerId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.Equal("Le panier est vide.", problem.Detail);
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
