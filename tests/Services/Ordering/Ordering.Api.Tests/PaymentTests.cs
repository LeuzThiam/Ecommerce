using System.Net;
using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class PaymentTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreatePaymentIntent_ForPendingOrder_ReturnsClientSecret()
    {
        var (orderId, _) = await CreateOrderWithPaymentIntentAsync();

        var order = await GetOrderAsync(orderId);
        Assert.Equal("PendingPayment", order!.Status);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_DoesNotChangeOrderStatus()
    {
        var (orderId, providerPaymentId) = await CreateOrderWithPaymentIntentAsync();

        var response = await SendWebhookAsync($"evt_1|payment_intent.succeeded|{providerPaymentId}", "wrong-signature");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await GetOrderAsync(orderId);
        Assert.Equal("PendingPayment", order!.Status);
    }

    [Fact]
    public async Task Webhook_PaymentSucceeded_MarksOrderAsPaid()
    {
        var (orderId, providerPaymentId) = await CreateOrderWithPaymentIntentAsync();

        await SendWebhookAsync($"evt_2|payment_intent.succeeded|{providerPaymentId}", FakePaymentGateway.ValidSignature);

        var order = await GetOrderAsync(orderId);
        Assert.Equal("Paid", order!.Status);
    }

    [Fact]
    public async Task Webhook_PaymentFailed_CancelsOrderAndReleasesStock()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 3));
        factory.StockClient.SetStock(productId, 3);
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 2 });
        var checkoutResponse = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();
        var intentResponse = await _client.PostAsync($"/api/ordering/orders/{order!.Id}/payment-intent", null);
        var intent = await intentResponse.Content.ReadFromJsonAsync<PaymentIntentDto>();
        var providerPaymentId = intent!.ClientSecret.Replace("_secret", "");

        await SendWebhookAsync($"evt_3|payment_intent.payment_failed|{providerPaymentId}", FakePaymentGateway.ValidSignature);

        var updatedOrder = await GetOrderAsync(order.Id);
        Assert.Equal("Cancelled", updatedOrder!.Status);
        Assert.Contains(factory.StockClient.ReleasedReservations, r => r.ProductId == productId && r.Quantity == 2);
    }

    [Fact]
    public async Task Webhook_ReplayedEvent_IsProcessedOnlyOnce()
    {
        var (orderId, providerPaymentId) = await CreateOrderWithPaymentIntentAsync();
        var webhookPayload = $"evt_4|payment_intent.succeeded|{providerPaymentId}";

        var firstResponse = await SendWebhookAsync(webhookPayload, FakePaymentGateway.ValidSignature);
        var secondResponse = await SendWebhookAsync(webhookPayload, FakePaymentGateway.ValidSignature);

        // Si l'idempotence ne fonctionnait pas, le 2e appel tenterait de reconfirmer un paiement
        // déjà confirmé — Payment.MarkAsSucceeded lèverait une DomainException, non interceptée
        // par l'endpoint webhook, ce qui donnerait un 500 plutôt qu'un 200.
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var order = await GetOrderAsync(orderId);
        Assert.Equal("Paid", order!.Status);
    }

    private async Task<(Guid OrderId, string ProviderPaymentId)> CreateOrderWithPaymentIntentAsync()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit", 10m, 5));
        factory.StockClient.SetStock(productId, 5);
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });

        var checkoutResponse = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();

        var intentResponse = await _client.PostAsync($"/api/ordering/orders/{order!.Id}/payment-intent", null);
        var intent = await intentResponse.Content.ReadFromJsonAsync<PaymentIntentDto>();
        var providerPaymentId = intent!.ClientSecret.Replace("_secret", "");

        return (order.Id, providerPaymentId);
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(string payload, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ordering/payments/webhook")
        {
            Content = new StringContent(payload)
        };
        request.Headers.Add("Stripe-Signature", signature);
        return await _client.SendAsync(request);
    }

    private async Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        var response = await _client.GetAsync($"/api/ordering/orders/{orderId}");
        return await response.Content.ReadFromJsonAsync<OrderDto>();
    }
}
