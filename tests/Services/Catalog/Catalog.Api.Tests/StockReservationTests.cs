using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public class StockReservationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Reserve_WithEnoughStock_ReturnsOk()
    {
        var productId = await CreateProductAsync(stock: 5);

        var response = await _client.PostAsJsonAsync("/api/catalog/stock/reserve", new { productId, quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_WithInsufficientStock_ReturnsConflict()
    {
        var productId = await CreateProductAsync(stock: 2);

        var response = await _client.PostAsJsonAsync("/api/catalog/stock/reserve", new { productId, quantity = 5 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_TwoConcurrentRequestsOnLastUnit_OnlyOneSucceeds()
    {
        // Scénario Gherkin de l'Issue #3 : un seul article en stock, deux réservations
        // simultanées de 1 unité chacune ne doivent jamais toutes les deux réussir.
        var productId = await CreateProductAsync(stock: 1);

        var firstAttempt = _client.PostAsJsonAsync("/api/catalog/stock/reserve", new { productId, quantity = 1 });
        var secondAttempt = _client.PostAsJsonAsync("/api/catalog/stock/reserve", new { productId, quantity = 1 });

        var responses = await Task.WhenAll(firstAttempt, secondAttempt);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);
    }

    private async Task<Guid> CreateProductAsync(int stock)
    {
        var categoryResponse = await _client.PostAsJsonAsync("/api/catalog/categories", new { name = $"Categorie-{Guid.NewGuid()}" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var productResponse = await _client.PostAsJsonAsync("/api/catalog/products", new
        {
            name = $"Produit-{Guid.NewGuid()}",
            price = 10m,
            stock,
            categoryId = category!.Id
        });

        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();
        return product!.Id;
    }

    private sealed record CategoryResponse(Guid Id, string Name);
    private sealed record ProductResponse(Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId);
}
