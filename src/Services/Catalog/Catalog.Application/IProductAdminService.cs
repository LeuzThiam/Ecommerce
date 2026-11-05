namespace Catalog.Application;

public interface IProductAdminService
{
    Task<ProductDto> CreateAsync(string name, decimal price, int stock, Guid categoryId, CancellationToken cancellationToken);
    Task<ProductDto?> UpdateAsync(Guid id, string name, decimal price, int stock, Guid categoryId, CancellationToken cancellationToken);
    Task<ProductDto?> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDto?> ActivateAsync(Guid id, CancellationToken cancellationToken);
}
