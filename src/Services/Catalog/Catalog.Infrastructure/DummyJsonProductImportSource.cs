using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Catalog.Application;

namespace Catalog.Infrastructure;

// Traduit la forme brute de dummyjson.com (API publique gratuite de donnees fictives) vers notre
// propre ExternalProductDto - l'Application ne connait jamais ce format externe, exactement comme
// IPaymentGateway isole deja le SDK Stripe dans Ordering.Api.
public sealed class DummyJsonProductImportSource(HttpClient httpClient) : IDummyProductImportSource
{
    public async Task<IReadOnlyList<ExternalProductDto>> FetchAsync(int limit, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/products?limit={limit}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<DummyJsonResponse>(cancellationToken: cancellationToken);
        if (payload?.Products is null)
        {
            return [];
        }

        return payload.Products
            .Select(p => new ExternalProductDto(p.Title, p.Price, p.Stock, p.Category, p.Thumbnail))
            .ToList();
    }

    private sealed record DummyJsonResponse([property: JsonPropertyName("products")] List<DummyJsonProduct> Products);

    private sealed record DummyJsonProduct(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("price")] decimal Price,
        [property: JsonPropertyName("stock")] int Stock,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("thumbnail")] string? Thumbnail);
}
