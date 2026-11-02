namespace Catalog.Application;

public interface IProductAdminService
{
    Task<ProductDto> CreateAsync(string name, decimal price, int stock, Guid categoryId, CancellationToken cancellationToken);
}
