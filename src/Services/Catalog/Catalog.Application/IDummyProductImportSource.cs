namespace Catalog.Application;

public sealed record ExternalReviewDto(int Rating, string Comment, string ReviewerName, string ReviewerEmail, DateTime ReviewedAtUtc);

public sealed record ExternalProductDto(
    string Name,
    decimal Price,
    int Stock,
    string CategoryName,
    string? ImageUrl = null,
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
    IReadOnlyList<ExternalReviewDto>? Reviews = null);

public interface IDummyProductImportSource
{
    Task<IReadOnlyList<ExternalProductDto>> FetchAsync(int limit, CancellationToken cancellationToken);
}
