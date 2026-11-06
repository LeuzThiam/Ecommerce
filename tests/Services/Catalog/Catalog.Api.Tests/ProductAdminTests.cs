using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public class ProductAdminTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _adminClient = CreateAuthenticatedClient(factory, "Administrator");

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(role));
        return client;
    }

    [Fact]
    public async Task CreateProduct_WithValidData_AppearsInPublicCatalog()
    {
        var (productId, slug, categoryId) = await CreateProductAsync();

        var publicResponse = await _adminClient.GetAsync($"/api/catalog/products/{slug}");

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var product = await publicResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(productId, product!.Id);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task CreateProduct_AsClient_ReturnsForbidden()
    {
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");
        var categoryId = await CreateCategoryAsync();

        var response = await clientRoleClient.PostAsJsonAsync("/api/catalog/products", new
        {
            name = $"Produit-{Guid.NewGuid()}",
            price = 10m,
            stock = 5,
            categoryId
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
    {
        var (productId, _, _) = await CreateProductAsync();
        var newCategoryId = await CreateCategoryAsync();

        var response = await _adminClient.PutAsJsonAsync($"/api/catalog/products/{productId}", new
        {
            name = "Nom Mis À Jour",
            price = 99m,
            stock = 42,
            categoryId = newCategoryId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Nom Mis À Jour", product!.Name);
        Assert.Equal(99m, product.Price);
        Assert.Equal(42, product.Stock);
        Assert.Equal(newCategoryId, product.CategoryId);
    }

    [Fact]
    public async Task UpdateProduct_AsClient_ReturnsForbidden()
    {
        var (productId, _, categoryId) = await CreateProductAsync();
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.PutAsJsonAsync($"/api/catalog/products/{productId}", new
        {
            name = "Tentative",
            price = 10m,
            stock = 5,
            categoryId
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_WithUnknownId_ReturnsNotFound()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await _adminClient.PutAsJsonAsync($"/api/catalog/products/{Guid.NewGuid()}", new
        {
            name = "Produit",
            price = 10m,
            stock = 5,
            categoryId
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateProduct_DisappearsFromPublicCatalogButIsNotDeleted()
    {
        var (productId, slug, _) = await CreateProductAsync();

        var deactivateResponse = await _adminClient.PostAsync($"/api/catalog/products/{productId}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.False(deactivated!.IsActive);

        var bySlugResponse = await _adminClient.GetAsync($"/api/catalog/products/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, bySlugResponse.StatusCode);

        var byIdResponse = await _adminClient.GetAsync($"/api/catalog/products/by-id/{productId}");
        Assert.Equal(HttpStatusCode.NotFound, byIdResponse.StatusCode);

        var searchResponse = await _adminClient.GetAsync("/api/catalog/products?sort=name&page=1&pageSize=100");
        var search = await searchResponse.Content.ReadFromJsonAsync<SearchResponse>();
        Assert.DoesNotContain(search!.Items, p => p.Id == productId);
    }

    [Fact]
    public async Task SearchProductsAdmin_IncludesDeactivatedProducts()
    {
        var (productId, _, _) = await CreateProductAsync();
        await _adminClient.PostAsync($"/api/catalog/products/{productId}/deactivate", null);

        var response = await _adminClient.GetAsync("/api/catalog/admin/products?sort=name&page=1&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var search = await response.Content.ReadFromJsonAsync<SearchResponse>();
        Assert.Contains(search!.Items, p => p.Id == productId && !p.IsActive);
    }

    [Fact]
    public async Task SearchProductsAdmin_AsClient_ReturnsForbidden()
    {
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.GetAsync("/api/catalog/admin/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProductByIdAdmin_ForDeactivatedProduct_ReturnsProduct()
    {
        var (productId, _, _) = await CreateProductAsync();
        await _adminClient.PostAsync($"/api/catalog/products/{productId}/deactivate", null);

        var response = await _adminClient.GetAsync($"/api/catalog/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.False(product!.IsActive);
    }

    [Fact]
    public async Task GetProductByIdAdmin_AsClient_ReturnsForbidden()
    {
        var (productId, _, _) = await CreateProductAsync();
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.GetAsync($"/api/catalog/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateProduct_AsClient_ReturnsForbidden()
    {
        var (productId, _, _) = await CreateProductAsync();
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.PostAsync($"/api/catalog/products/{productId}/deactivate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ActivateProduct_AfterDeactivate_ReappearsInPublicCatalog()
    {
        var (productId, slug, _) = await CreateProductAsync();
        await _adminClient.PostAsync($"/api/catalog/products/{productId}/deactivate", null);

        var activateResponse = await _adminClient.PostAsync($"/api/catalog/products/{productId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        var bySlugResponse = await _adminClient.GetAsync($"/api/catalog/products/{slug}");
        Assert.Equal(HttpStatusCode.OK, bySlugResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_WithValidName_ReturnsUpdatedCategory()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await _adminClient.PutAsJsonAsync($"/api/catalog/categories/{categoryId}", new { name = "Nouveau Nom" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal("Nouveau Nom", category!.Name);
    }

    [Fact]
    public async Task UpdateCategory_AsClient_ReturnsForbidden()
    {
        var categoryId = await CreateCategoryAsync();
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.PutAsJsonAsync($"/api/catalog/categories/{categoryId}", new { name = "Tentative" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> CreateCategoryAsync()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/catalog/categories", new { name = $"Categorie-{Guid.NewGuid()}" });
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return category!.Id;
    }

    private async Task<(Guid Id, string Slug, Guid CategoryId)> CreateProductAsync()
    {
        var categoryId = await CreateCategoryAsync();

        var response = await _adminClient.PostAsJsonAsync("/api/catalog/products", new
        {
            name = $"Produit-{Guid.NewGuid()}",
            price = 10m,
            stock = 5,
            categoryId
        });

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return (product!.Id, product.Slug, categoryId);
    }

    private sealed record CategoryResponse(Guid Id, string Name);
    private sealed record ProductResponse(Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId, bool IsActive);
    private sealed record SearchResponse(List<ProductResponse> Items, int Page, int PageSize, int TotalCount);
}
