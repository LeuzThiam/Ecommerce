namespace Catalog.Domain;

// Regroupe les attributs de catalogue "riches" venant d'une source externe (dummyjson.com) qui
// n'ont pas leur place dans Create/Update (saisis par un admin) : aucun admin ne tape à la main
// un poids, des dimensions ou un code-barres. Alimentés uniquement par l'import, jamais modifiés
// manuellement - d'où un type dédié plutôt que d'alourdir la signature de Update().
public sealed record ProductExternalDetails(
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
    IReadOnlyList<string>? Images = null);
