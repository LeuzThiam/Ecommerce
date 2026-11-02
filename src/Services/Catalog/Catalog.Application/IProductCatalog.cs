using Catalog.Application.Common;

namespace Catalog.Application;

public interface IProductCatalog
{
    Task<PagedResult<ProductDto>> SearchAsync(ProductSearch search, CancellationToken cancellationToken);
    Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
