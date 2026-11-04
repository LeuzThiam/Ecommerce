using System.Net.Http.Json;
using Ecommerce.Web.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace Ecommerce.Web.Clients;

public sealed class CatalogApiClient(HttpClient httpClient) : ICatalogApiClient
{
    public async Task<PagedResult<ProductDto>> SearchProductsAsync(
        string? query,
        Guid? categoryId,
        int page,
        CancellationToken cancellationToken)
    {
        var requestUri = QueryHelpers.AddQueryString("/api/catalog/products", new Dictionary<string, string?>
        {
            ["query"] = query,
            ["categoryId"] = categoryId?.ToString(),
            ["sort"] = "name",
            ["page"] = page.ToString(),
            ["pageSize"] = "20",
        });

        var result = await httpClient.GetFromJsonAsync<PagedResult<ProductDto>>(requestUri, cancellationToken);
        return result ?? new PagedResult<ProductDto>([], page, 20, 0, 0);
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/api/catalog/products/{slug}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await httpClient.GetFromJsonAsync<List<CategoryDto>>("/api/catalog/categories", cancellationToken);
        return categories ?? [];
    }
}
