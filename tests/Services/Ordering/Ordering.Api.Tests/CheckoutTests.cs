using System.Net;
using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class CheckoutTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Checkout_WithAvailableStock_CreatesOrderAndClearsCart()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 5));
        factory.StockClient.SetStock(productId, 5);
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 2 });

        var response = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("PendingPayment", order!.Status);
        Assert.Equal(20m, order.Total);

        var cartResponse = await _client.GetAsync($"/api/ordering/cart/{customerId}");
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Checkout_ProductUnavailableSinceAddedToCart_ReturnsBadRequest()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 1));
        factory.StockClient.SetStock(productId, 1);
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });

        // Quelqu'un d'autre épuise le stock entre l'ajout au panier et le checkout.
        factory.StockClient.SetStock(productId, 0);

        var response = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_SecondItemFails_ReleasesFirstItemReservation()
    {
        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productAId, "Produit A", 10m, 5));
        factory.CatalogClient.AddProduct(new ProductSnapshot(productBId, "Produit B", 10m, 2));
        factory.StockClient.SetStock(productAId, 5);
        factory.StockClient.SetStock(productBId, 2);
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId = productAId, quantity = 2 });
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId = productBId, quantity = 2 });

        // B devient indisponible juste avant le checkout — A, lui, reste disponible.
        factory.StockClient.SetStock(productBId, 0);

        var response = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(factory.StockClient.ReleasedReservations, r => r.ProductId == productAId && r.Quantity == 2);
    }
}
