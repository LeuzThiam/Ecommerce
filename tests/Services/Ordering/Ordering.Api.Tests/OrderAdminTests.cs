using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class OrderAdminTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllOrders_AsAdmin_ReturnsOrdersFromEveryCustomer()
    {
        var firstCustomerOrder = await CreateOrderAsync(Guid.NewGuid());
        var secondCustomerOrder = await CreateOrderAsync(Guid.NewGuid());
        var adminClient = CreateAuthenticatedClient("Administrator");

        var response = await adminClient.GetAsync("/api/ordering/admin/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.Contains(orders!, o => o.Id == firstCustomerOrder.Id);
        Assert.Contains(orders!, o => o.Id == secondCustomerOrder.Id);
    }

    [Fact]
    public async Task GetAllOrders_AsClient_ReturnsForbidden()
    {
        var clientClient = CreateAuthenticatedClient("Client");

        var response = await clientClient.GetAsync("/api/ordering/admin/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllOrders_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/ordering/admin/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderAdmin_BelongingToAnotherCustomer_ReturnsOrder()
    {
        var order = await CreateOrderAsync(Guid.NewGuid());
        var adminClient = CreateAuthenticatedClient("Administrator");

        var response = await adminClient.GetAsync($"/api/ordering/admin/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(order.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetOrderAdmin_AsClient_ReturnsForbidden()
    {
        var order = await CreateOrderAsync(Guid.NewGuid());
        var clientClient = CreateAuthenticatedClient("Client");

        var response = await clientClient.GetAsync($"/api/ordering/admin/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderAdmin_WithUnknownId_ReturnsNotFound()
    {
        var adminClient = CreateAuthenticatedClient("Administrator");

        var response = await adminClient.GetAsync($"/api/ordering/admin/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(Guid.NewGuid(), role));
        return client;
    }

    private async Task<OrderDto> CreateOrderAsync(Guid customerId)
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 5));
        factory.StockClient.SetStock(productId, 5);

        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });
        var response = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);

        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }
}
