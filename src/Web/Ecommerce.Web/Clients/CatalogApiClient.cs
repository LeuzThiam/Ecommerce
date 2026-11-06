using System.Net;
using System.Net.Http.Headers;
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

    public async Task<PagedResult<ProductDto>> SearchAllProductsAsync(
        string accessToken,
        string? query,
        Guid? categoryId,
        int page,
        CancellationToken cancellationToken)
    {
        var requestUri = QueryHelpers.AddQueryString("/api/catalog/admin/products", new Dictionary<string, string?>
        {
            ["query"] = query,
            ["categoryId"] = categoryId?.ToString(),
            ["sort"] = "name",
            ["page"] = page.ToString(),
            ["pageSize"] = "20",
        });

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, requestUri, accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>(cancellationToken);
        return result ?? new PagedResult<ProductDto>([], page, 20, 0, 0);
    }

    public async Task<ProductDto?> GetProductByIdAsync(string accessToken, Guid id, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/catalog/admin/products/{id}", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    public async Task<ProductDto> CreateProductAsync(
        string accessToken,
        string name,
        decimal price,
        int stock,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/catalog/products", accessToken);
        request.Content = JsonContent.Create(new { name, price, stock, categoryId });
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new CatalogApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken))!;
    }

    public async Task<ProductDto?> UpdateProductAsync(
        string accessToken,
        Guid id,
        string name,
        decimal price,
        int stock,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/catalog/products/{id}", accessToken);
        request.Content = JsonContent.Create(new { name, price, stock, categoryId });
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new CatalogApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    public async Task<ProductDto?> DeactivateProductAsync(string accessToken, Guid id, CancellationToken cancellationToken) =>
        await PostAdminActionAsync($"/api/catalog/products/{id}/deactivate", accessToken, cancellationToken);

    public async Task<ProductDto?> ActivateProductAsync(string accessToken, Guid id, CancellationToken cancellationToken) =>
        await PostAdminActionAsync($"/api/catalog/products/{id}/activate", accessToken, cancellationToken);

    public async Task<CategoryDto> CreateCategoryAsync(string accessToken, string name, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/catalog/categories", accessToken);
        request.Content = JsonContent.Create(new { name });
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new CatalogApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken))!;
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(string accessToken, Guid id, string name, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/catalog/categories/{id}", accessToken);
        request.Content = JsonContent.Create(new { name });
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new CatalogApiException(await ReadErrorMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken);
    }

    private async Task<ProductDto?> PostAdminActionAsync(string uri, string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, uri, accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    // HttpClient.DefaultRequestHeaders est mutable et partagé entre requêtes concurrentes via le
    // pool du HttpClientFactory - on pose donc l'en-tête Authorization par requête plutôt que dessus.
    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string uri, string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    // Depuis l'Issue #21, Catalog.Api traduit les DomainException en Problem Details (RFC 7807)
    // via un gestionnaire d'exceptions global - le message lisible se trouve dans "detail".
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(cancellationToken);
        return problem?.Detail ?? "La requête a été refusée par le service de catalogue.";
    }

    private sealed record ProblemDetailsResponse(string Detail);
}
