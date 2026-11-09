using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catalog.Application;

namespace Catalog.Api.Tests;

public class ProductImportTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _adminClient = CreateAuthenticatedClient(factory, "Administrator");

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.GenerateToken(role));
        return client;
    }

    [Fact]
    public async Task Import_WithNewProducts_CreatesProductsAndRealCategoryRelation()
    {
        var categoryName = $"Categorie-Import-{Guid.NewGuid()}";
        factory.ImportSource.SetProducts(
            new ExternalProductDto($"Produit-Import-{Guid.NewGuid()}", 12.50m, 7, categoryName));

        var response = await _adminClient.PostAsync("/api/catalog/admin/products/import-dummy?limit=20", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProductImportResultDto>();
        Assert.Equal(1, result!.Imported);
        Assert.Equal(0, result.Skipped);

        // La catégorie créée doit être une vraie Category, pas une chaîne libre comme dans
        // l'ancien DummyJsonService - on vérifie qu'elle apparaît bien dans la liste des
        // catégories administrables.
        var categoriesResponse = await _adminClient.GetAsync("/api/catalog/categories");
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Contains(categories!, c => c.Name == categoryName);
    }

    [Fact]
    public async Task Import_ReimportingSameProduct_SkipsDuplicate()
    {
        var productName = $"Produit-Dup-{Guid.NewGuid()}";
        factory.ImportSource.SetProducts(new ExternalProductDto(productName, 9.99m, 3, "Categorie-Dup"));

        await _adminClient.PostAsync("/api/catalog/admin/products/import-dummy", null);
        var secondResponse = await _adminClient.PostAsync("/api/catalog/admin/products/import-dummy", null);

        var result = await secondResponse.Content.ReadFromJsonAsync<ProductImportResultDto>();
        Assert.Equal(0, result!.Imported);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public async Task Import_WithPreExistingDuplicateCategoryNames_DoesNotFail()
    {
        // Rien dans le Domain n'empêche deux catégories de partager le même nom (pas d'index
        // unique) - un vrai cas rencontré en conditions réelles, pas juste théorique.
        var duplicateName = $"Categorie-Doublon-{Guid.NewGuid()}";
        await _adminClient.PostAsJsonAsync("/api/catalog/categories", new { name = duplicateName });
        await _adminClient.PostAsJsonAsync("/api/catalog/categories", new { name = duplicateName });

        factory.ImportSource.SetProducts(
            new ExternalProductDto($"Produit-Avec-Doublon-{Guid.NewGuid()}", 5m, 1, duplicateName));

        var response = await _adminClient.PostAsync("/api/catalog/admin/products/import-dummy", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProductImportResultDto>();
        Assert.Equal(1, result!.Imported);
    }

    [Fact]
    public async Task Import_AsClient_ReturnsForbidden()
    {
        var clientRoleClient = CreateAuthenticatedClient(factory, "Client");

        var response = await clientRoleClient.PostAsync("/api/catalog/admin/products/import-dummy", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Import_WithoutToken_ReturnsUnauthorized()
    {
        using var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.PostAsync("/api/catalog/admin/products/import-dummy", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record CategoryResponse(Guid Id, string Name);
}
