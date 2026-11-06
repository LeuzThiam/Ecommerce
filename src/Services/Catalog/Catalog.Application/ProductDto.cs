namespace Catalog.Application;

public sealed record ProductVariantDto(Guid Id, string Name, decimal Price, int Stock);

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    int Stock,
    Guid CategoryId,
    bool IsActive,
    IReadOnlyList<ProductVariantDto>? Variants = null);
