using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Ordering.Application;

namespace Ordering.Api.Tests;

public class InvoiceTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DownloadInvoice_ForPaidOrder_ReturnsPdf()
    {
        var (customerId, orderId) = await CreateAndPayOrderAsync();

        var response = await GetAuthenticatedAsync(customerId, $"/api/ordering/orders/{orderId}/invoice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task DownloadInvoice_ForUnpaidOrder_ReturnsBadRequest()
    {
        var (customerId, orderId) = await CreateOrderAsync();

        var response = await GetAuthenticatedAsync(customerId, $"/api/ordering/orders/{orderId}/invoice");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadInvoice_BelongingToAnotherCustomer_ReturnsNotFound()
    {
        var (_, orderId) = await CreateAndPayOrderAsync();
        var someoneElseId = Guid.NewGuid();

        var response = await GetAuthenticatedAsync(someoneElseId, $"/api/ordering/orders/{orderId}/invoice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadInvoice_WithoutToken_ReturnsUnauthorized()
    {
        var (_, orderId) = await CreateAndPayOrderAsync();
        using var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.GetAsync($"/api/ordering/orders/{orderId}/invoice");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DownloadInvoice_AfterProductPriceChangesInCatalog_StillReflectsOriginalPrice()
    {
        var productId = Guid.NewGuid();
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit prix changeant", 10m, 5));
        factory.StockClient.SetStock(productId, 5);
        var customerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(customerId));
        await _client.PostAsJsonAsync($"/api/ordering/cart/{customerId}/items", new { productId, quantity = 1 });
        var checkoutResponse = await _client.PostAsync($"/api/ordering/checkout/{customerId}", null);
        var order = (await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>())!;
        await PayOrderAsync(order.Id);

        // Le prix catalogue change APRÈS l'émission de la commande - l'OrderItem a déjà capturé
        // le prix payé (10) et ne le re-consulte jamais auprès du catalogue.
        factory.CatalogClient.AddProduct(new ProductSnapshot(productId, "Produit prix changeant", 999m, 5));

        var orderAfterPriceChange = await GetAuthenticatedAsync(customerId, $"/api/ordering/orders/{order.Id}");
        var orderDto = await orderAfterPriceChange.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(10m, orderDto!.Items[0].UnitPrice);

        var invoiceResponse = await GetAuthenticatedAsync(customerId, $"/api/ordering/orders/{order.Id}/invoice");
        Assert.Equal(HttpStatusCode.OK, invoiceResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(Guid customerId, string path)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(customerId));
        return await client.GetAsync(path);
    }

    private async Task PayOrderAsync(Guid orderId)
    {
        var intentResponse = await _client.PostAsync($"/api/ordering/orders/{orderId}/payment-intent", null);
        var intent = await intentResponse.Content.ReadFromJsonAsync<PaymentIntentDto>();
        var providerPaymentId = intent!.ClientSecret.Replace("_secret", "");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ordering/payments/webhook")
        {
            Content = new StringContent($"evt_invoice_{orderId}|payment_intent.succeeded|{providerPaymentId}")
        };
        request.Headers.Add("Stripe-Signature", FakePaymentGateway.ValidSignature);
        await _client.SendAsync(request);
    }

    private async Task<(Guid CustomerId, Guid OrderId)> CreateAndPayOrderAsync()
    {
        var (customerId, orderId) = await CreateOrderAsync();
        await PayOrderAsync(orderId);
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
}
