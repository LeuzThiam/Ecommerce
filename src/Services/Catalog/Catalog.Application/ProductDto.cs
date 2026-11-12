namespace Catalog.Application;

public sealed record ProductVariantDto(Guid Id, string Name, decimal Price, int Stock);

public sealed record ProductReviewDto(Guid Id, int Rating, string Comment, string ReviewerName, string ReviewerEmail, DateTime ReviewedAtUtc);

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    int Stock,
    Guid CategoryId,
    bool IsActive,
    string? ImageUrl = null,
    DateTime CreatedAtUtc = default,
    // Attributs riches, détaillés dans ProductExternalDetails (Domain) - remplis uniquement sur
    // les lectures à l'unité (GetBySlug/GetById), comme Variants : lourds et inutiles en liste.
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
