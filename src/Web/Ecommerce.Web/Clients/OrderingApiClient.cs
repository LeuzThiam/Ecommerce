using System.Net;
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
