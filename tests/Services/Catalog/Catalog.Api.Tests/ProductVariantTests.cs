using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public class ProductVariantTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _adminClient = CreateAuthenticatedClient(factory, "Administrator");

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(role));
        return client;
    }

    [Fact]
    public async Task AddVariant_WithValidData_AppearsOnProductDetail()
    {
        var (productId, slug) = await CreateProductAsync();

        var addResponse = await _adminClient.PostAsJsonAsync($"/api/catalog/products/{productId}/variants", new
        {
            name = "Rouge - M",
            price = 24.99m,
            stock = 5
        });

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var detailResponse = await _adminClient.GetAsync($"/api/catalog/products/{slug}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Single(detail!.Variants!);
        Assert.Equal("Rouge - M", detail.Variants![0].Name);
        Assert.Equal(24.99m, detail.Variants[0].Price);
        Assert.Equal(5, detail.Variants[0].Stock);
    }

    [Fact]
    public async Task AddVariant_AsClient_ReturnsForbidden()
    {
        var (productId, _) = await CreateProductAsync();
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.PostAsJsonAsync($"/api/catalog/products/{productId}/variants", new
        {
            name = "Rouge - M",
            price = 24.99m,
            stock = 5
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddVariant_WithUnknownProductId_ReturnsNotFound()
    {
        var response = await _adminClient.PostAsJsonAsync($"/api/catalog/products/{Guid.NewGuid()}/variants", new
        {
            name = "Rouge - M",
            price = 24.99m,
            stock = 5
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddVariant_WithNegativePrice_ReturnsBadRequest()
    {
        var (productId, _) = await CreateProductAsync();

        var response = await _adminClient.PostAsJsonAsync($"/api/catalog/products/{productId}/variants", new
        {
            name = "Rouge - M",
            price = -1m,
            stock = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TwoVariantsOfSameProduct_HaveIndependentPriceAndStock()
    {
        var (productId, slug) = await CreateProductAsync();

        await _adminClient.PostAsJsonAsync($"/api/catalog/products/{productId}/variants", new { name = "Rouge - S", price = 19.99m, stock = 0 });
        await _adminClient.PostAsJsonAsync($"/api/catalog/products/{productId}/variants", new { name = "Rouge - M", price = 24.99m, stock = 8 });

        var detailResponse = await _adminClient.GetAsync($"/api/catalog/products/{slug}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var variants = detail!.Variants!;
        var small = variants.Single(v => v.Name == "Rouge - S");
        var medium = variants.Single(v => v.Name == "Rouge - M");

        // Scénario Gherkin "Stock indépendant par variante" (Issue #11) : la variante en
        // rupture (S) et la variante disponible (M) du même produit ont chacune leur propre
        // stock et leur propre prix, sans interférence entre les deux.
        Assert.Equal(0, small.Stock);
        Assert.Equal(8, medium.Stock);
        Assert.NotEqual(small.Price, medium.Price);
    }

    private async Task<(Guid Id, string Slug)> CreateProductAsync()
    {
        var categoryResponse = await _adminClient.PostAsJsonAsync("/api/catalog/categories", new { name = $"Categorie-{Guid.NewGuid()}" });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var productResponse = await _adminClient.PostAsJsonAsync("/api/catalog/products", new
        {
            name = $"Produit-{Guid.NewGuid()}",
            price = 10m,
            stock = 5,
            categoryId = category!.Id
        });

        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();
        return (product!.Id, product.Slug);
    }

    private sealed record CategoryResponse(Guid Id, string Name);
    private sealed record ProductVariantResponse(Guid Id, string Name, decimal Price, int Stock);
    private sealed record ProductResponse(Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId, bool IsActive, List<ProductVariantResponse>? Variants);
}
