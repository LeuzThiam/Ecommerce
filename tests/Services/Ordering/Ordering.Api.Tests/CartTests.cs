using System.Net;
using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class CartTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AddItem_ValidProduct_ReturnsOkWithCatalogPriceAndName()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit Test", 15.50m, 10));
        var customerId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        var item = Assert.Single(cart!.Items);
        Assert.Equal("Produit Test", item.ProductName);
        Assert.Equal(15.50m, item.UnitPrice);
        Assert.Equal(31.00m, cart.Total);
    }

    [Fact]
    public async Task AddItem_OutOfStockProduct_ReturnsBadRequest()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit Epuise", 10m, 0));
        var customerId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_ExistingItem_UpdatesQuantity()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 5m, 10));
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });

        var response = await _client.PutAsJsonAsync($"/api/ordering/cart/{customerId}/items/{productId}", new { quantity = 4 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.Equal(4, cart!.Items.Single().Quantity);
    }
}
