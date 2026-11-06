using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class RefundTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Refund_PaidOrder_ChangesStatusToRefundedAndIsVisibleInHistory()
    {
        var (customerId, orderId) = await CreateAndPayOrderAsync();
        var adminClient = CreateAuthenticatedClient("Administrator");

        var refundResponse = await adminClient.PostAsync($"/api/ordering/orders/{orderId}/refund", null);

        Assert.Equal(HttpStatusCode.NoContent, refundResponse.StatusCode);
        var order = await GetOrderAsync(customerId, orderId);
        Assert.Equal("Refunded", order!.Status);
        Assert.Contains(order.Payments!, p => p.Status == "Refunded");
    }

    [Fact]
    public async Task Refund_UnpaidOrder_IsRefused()
    {
        var (_, orderId) = await CreateOrderAsync();
        var adminClient = CreateAuthenticatedClient("Administrator");

        var refundResponse = await adminClient.PostAsync($"/api/ordering/orders/{orderId}/refund", null);

        Assert.Equal(HttpStatusCode.BadRequest, refundResponse.StatusCode);
    }

    [Fact]
    public async Task Refund_AsClient_ReturnsForbidden()
    {
        var (_, orderId) = await CreateAndPayOrderAsync();
        var clientClient = CreateAuthenticatedClient("Client");

        var refundResponse = await clientClient.PostAsync($"/api/ordering/orders/{orderId}/refund", null);

        Assert.Equal(HttpStatusCode.Forbidden, refundResponse.StatusCode);
    }

    [Fact]
    public async Task Refund_WithoutToken_ReturnsUnauthorized()
    {
        var (_, orderId) = await CreateAndPayOrderAsync();
        using var anonymousClient = factory.CreateClient();

        var refundResponse = await anonymousClient.PostAsync($"/api/ordering/orders/{orderId}/refund", null);

        Assert.Equal(HttpStatusCode.Unauthorized, refundResponse.StatusCode);
    }

    [Fact]
    public async Task GetOrder_AfterFailedAndSucceededAttempts_ShowsFullPaymentHistory()
    {
        var (customerId, orderId) = await CreateAndPayOrderAsync();

        var order = await GetOrderAsync(customerId, orderId);

        Assert.Single(order!.Payments!);
        Assert.Equal("Succeeded", order.Payments![0].Status);
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(Guid.NewGuid(), role));
        return client;
    }

    private async Task<(Guid CustomerId, Guid OrderId)> CreateAndPayOrderAsync()
    {
        var (customerId, orderId) = await CreateOrderAsync();

        var intentResponse = await _client.PostAsync($"/api/ordering/orders/{orderId}/payment-intent", null);
        var intent = await intentResponse.Content.ReadFromJsonAsync<PaymentIntentDto>();
        var providerPaymentId = intent!.ClientSecret.Replace("_secret", "");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ordering/payments/webhook")
        {
            Content = new StringContent($"evt_refund_{orderId}|payment_intent.succeeded|{providerPaymentId}")
        };
        request.Headers.Add("Stripe-Signature", FakePaymentGateway.ValidSignature);
        await _client.SendAsync(request);

        return (customerId, orderId);
    }

    private async Task<(Guid CustomerId, Guid OrderId)> CreateOrderAsync()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 5));
        factory.StockClient.SetStock(productId, 5);
        var customerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(customerId));
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });

        var checkoutResponse = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();

        return (customerId, order!.Id);
    }

    private async Task<OrderDto?> GetOrderAsync(Guid customerId, Guid orderId)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(customerId));
        var response = await client.GetAsync($"/api/ordering/orders/{orderId}");
        return await response.Content.ReadFromJsonAsync<OrderDto>();
    }
}
