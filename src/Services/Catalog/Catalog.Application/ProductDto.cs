namespace Catalog.Application;

public sealed record ProductDto(Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId);
