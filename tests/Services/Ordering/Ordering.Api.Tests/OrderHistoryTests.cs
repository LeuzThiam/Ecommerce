using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class OrderHistoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetOrderHistory_ReturnsAllOrdersForAuthenticatedCustomer()
    {
        var customerId = Guid.NewGuid();
        await CheckoutOneItemAsync(customerId);
        await CheckoutOneItemAsync(customerId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(customerId));
        var response = await _client.GetAsync("/api/ordering/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.Equal(2, orders!.Count);
        Assert.All(orders, o => Assert.Equal(customerId, o.CustomerId));
    }

    [Fact]
    public async Task GetOrderHistory_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/ordering/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_OwnOrder_ReturnsOrder()
    {
        var customerId = Guid.NewGuid();
        var order = await CheckoutOneItemAsync(customerId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(customerId));
        var response = await _client.GetAsync($"/api/ordering/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(order.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetOrder_BelongingToAnotherCustomer_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var order = await CheckoutOneItemAsync(ownerId);

        var someoneElseId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(someoneElseId));
        var response = await _client.GetAsync($"/api/ordering/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_WithoutToken_ReturnsUnauthorized()
    {
        var customerId = Guid.NewGuid();
        var order = await CheckoutOneItemAsync(customerId);

        using var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/ordering/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<OrderDto> CheckoutOneItemAsync(Guid customerId)
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 5));
        factory.StockClient.SetStock(productId, 5);

        // Le panier et le checkout restent anonymes (route customerId) - seule la consultation
        // de l'historique exige un token, donc pas besoin d'en poser un ici.
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });
        var response = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);

        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }
}
