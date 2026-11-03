using System.Net.Http.Json;
using Ordering.Application;

namespace Ordering.Infrastructure;

public sealed class HttpProductCatalogClient(HttpClient httpClient) : IProductCatalogClient
{
    public async Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/api/catalog/products/by-id/{productId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var product = await response.Content.ReadFromJsonAsync<CatalogProductResponse>(cancellationToken);
        return product is null ? null : new ProductSnapshot(product.Id, product.Name, product.Price, product.Stock);
    }

    private sealed record CatalogProductResponse(Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId);
}
