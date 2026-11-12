namespace Ecommerce.Web.Models;

// Ces types reproduisent volontairement la forme des réponses de Catalog.Api — le Web ne
// référence aucun projet Application/Domain des services backend (ce serait coupler deux
// services indépendants). Il consomme leur API REST comme n'importe quel client externe.
public sealed record ProductDto(
    Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId, bool IsActive = true, string? ImageUrl = null,
    DateTime CreatedAtUtc = default,
    string? Description = null,
    string? Brand = null,
    string? Sku = null,
    decimal? DiscountPercentage = null,
    decimal? Rating = null,
    decimal? Weight = null,
    decimal? DimensionWidth = null,
    decimal? DimensionHeight = null,
    decimal? DimensionDepth = null,
    string? WarrantyInformation = null,
    string? ShippingInformation = null,
    string? AvailabilityStatus = null,
    string? ReturnPolicy = null,
    int? MinimumOrderQuantity = null,
    string? Barcode = null,
    string? QrCodeUrl = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Images = null,
    IReadOnlyList<ProductReviewDto>? Reviews = null,
    IReadOnlyList<ProductVariantDto>? Variants = null);

public sealed record ProductVariantDto(Guid Id, string Name, decimal Price, int Stock);

public sealed record ProductReviewDto(Guid Id, int Rating, string Comment, string ReviewerName, string ReviewerEmail, DateTime ReviewedAtUtc);

public sealed record CategoryDto(Guid Id, string Name);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
