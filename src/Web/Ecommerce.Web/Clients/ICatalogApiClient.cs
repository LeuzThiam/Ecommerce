using Ecommerce.Web.Models;

namespace Ecommerce.Web.Clients;

public interface ICatalogApiClient
{
    Task<PagedResult<ProductDto>> SearchProductsAsync(
        string? query,
        Guid? categoryId,
        int page,
        CancellationToken cancellationToken);

    Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);

    // Vue administrateur : inclut aussi les produits désactivés.
    Task<PagedResult<ProductDto>> SearchAllProductsAsync(
        string accessToken,
        string? query,
        Guid? categoryId,
        int page,
        CancellationToken cancellationToken);

    Task<ProductDto?> GetProductByIdAsync(string accessToken, Guid id, CancellationToken cancellationToken);

    Task<ProductDto> CreateProductAsync(
        string accessToken,
        string name,
        decimal price,
        int stock,
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<ProductDto?> UpdateProductAsync(
        string accessToken,
        Guid id,
        string name,
        decimal price,
        int stock,
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<ProductDto?> DeactivateProductAsync(string accessToken, Guid id, CancellationToken cancellationToken);

    Task<ProductDto?> ActivateProductAsync(string accessToken, Guid id, CancellationToken cancellationToken);

    Task<CategoryDto> CreateCategoryAsync(string accessToken, string name, CancellationToken cancellationToken);

    Task<CategoryDto?> UpdateCategoryAsync(string accessToken, Guid id, string name, CancellationToken cancellationToken);
}
