namespace Catalog.Application;

public interface IProductAdminService
{
    Task<ProductDto> CreateAsync(string name, decimal price, int stock, Guid categoryId, string? imageUrl, CancellationToken cancellationToken);
    Task<ProductDto?> UpdateAsync(Guid id, string name, decimal price, int stock, Guid categoryId, string? imageUrl, CancellationToken cancellationToken);
    Task<ProductDto?> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDto?> ActivateAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDto?> AddVariantAsync(Guid productId, string name, decimal price, int stock, CancellationToken cancellationToken);

    // Attributs riches (description, marque, dimensions, avis...) - remplis uniquement par
    // l'import dummyjson, jamais par un formulaire admin. Réutilise ExternalProductDto comme
    // porteur : c'est déjà exactement la forme que détient ProductImportService après le fetch.
    Task<ProductDto?> SetExternalDetailsAsync(Guid id, ExternalProductDto details, CancellationToken cancellationToken);
}
