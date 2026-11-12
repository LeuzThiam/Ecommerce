using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Web.Models;

namespace Ecommerce.Web.Clients;

public sealed class OrderingApiClient(HttpClient httpClient) : IOrderingApiClient
{
    public async Task<CartDto> GetCartAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var cart = await httpClient.GetFromJsonAsync<CartDto>($"/api/ordering/cart/{customerId}", cancellationToken);
        return cart ?? new CartDto(Guid.Empty, customerId, [], 0m);
    }

    public Task<CartDto?> AddItemAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken) =>
        PostOrPutAsync(
            () => httpClient.PostAsJsonAsync(
                $"/api/ordering/cart/{customerId}/items",
                new { ProductId = productId, Quantity = quantity },
                cancellationToken),
            cancellationToken);

    public Task<CartDto?> UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken) =>
        PostOrPutAsync(
            () => httpClient.PutAsJsonAsync(
                $"/api/ordering/cart/{customerId}/items/{productId}",
                new { Quantity = quantity },
                cancellationToken),
            cancellationToken);

    public Task<CartDto?> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken) =>
        PostOrPutAsync(
            () => httpClient.DeleteAsync($"/api/ordering/cart/{customerId}/items/{productId}", cancellationToken),
            cancellationToken);

    public async Task<OrderDto?> CheckoutAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"/api/ordering/checkout/{customerId}", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new OrderingApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
    }

    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"/api/ordering/orders/{orderId}/payment-intent", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new OrderingApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentIntentDto>(cancellationToken))!;
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrderHistoryAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/ordering/orders", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken);
        return orders ?? [];
    }

    public async Task<OrderDto?> GetOrderAsync(string accessToken, Guid orderId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/ordering/orders/{orderId}", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
    }

    public async Task<InvoiceDownload> GetInvoiceAsync(string accessToken, Guid orderId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/ordering/orders/{orderId}/invoice", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new InvoiceDownload(null, await ReadErrorMessageAsync(response, cancellationToken));
        }

        if (!response.IsSuccessStatusCode)
        {
            return new InvoiceDownload(null, null);
        }

        return new InvoiceDownload(await response.Content.ReadAsByteArrayAsync(cancellationToken), null);
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllOrdersAdminAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/ordering/admin/orders", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken);
        return orders ?? [];
    }

    public async Task<OrderDto?> GetOrderAdminAsync(string accessToken, Guid orderId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/ordering/admin/orders/{orderId}", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
    }

    public async Task RefundOrderAsync(string accessToken, Guid orderId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/ordering/orders/{orderId}/refund", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new OrderingApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
    }

    // HttpClient.DefaultRequestHeaders est mutable et partagé entre requêtes concurrentes via le
    // pool du HttpClientFactory - on pose donc l'en-tête Authorization par requête plutôt que dessus.
    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string uri, string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task<CartDto?> PostOrPutAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        var response = await send();
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new OrderingApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CartDto>(cancellationToken);
    }

    // Depuis l'Issue #21, Ordering.Api traduit les DomainException en Problem Details (RFC 7807)
    // via un gestionnaire d'exceptions global plutôt qu'un corps { message } ad-hoc par endpoint
    // - le message lisible se trouve désormais dans "detail".
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(cancellationToken);
        return problem?.Detail ?? "La requête a été refusée par le service de commandes.";
    }

    private sealed record ProblemDetailsResponse(string Detail);
}
