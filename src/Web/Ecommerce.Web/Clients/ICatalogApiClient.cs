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
}
