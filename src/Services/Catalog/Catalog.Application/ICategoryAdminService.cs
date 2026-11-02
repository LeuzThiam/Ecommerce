namespace Catalog.Application;

public interface ICategoryAdminService
{
    Task<CategoryDto> CreateAsync(string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken);
}
