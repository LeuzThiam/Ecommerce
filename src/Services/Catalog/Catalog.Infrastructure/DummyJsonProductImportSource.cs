using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Catalog.Application;

namespace Catalog.Infrastructure;

// Traduit la forme brute de dummyjson.com (API publique gratuite de donnees fictives) vers notre
// propre ExternalProductDto - l'Application ne connait jamais ce format externe, exactement comme
// IPaymentGateway isole deja le SDK Stripe dans Ordering.Api.
public sealed class DummyJsonProductImportSource(HttpClient httpClient) : IDummyProductImportSource
{
    public async Task<IReadOnlyList<ExternalProductDto>> FetchAsync(int limit, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/products?limit={limit}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<DummyJsonResponse>(cancellationToken: cancellationToken);
        if (payload?.Products is null)
        {
            return [];
        }

        return payload.Products.Select(ToExternalProductDto).ToList();
    }

    private static ExternalProductDto ToExternalProductDto(DummyJsonProduct p) => new(
        p.Title,
        p.Price,
        p.Stock,
        p.Category,
        p.Thumbnail,
        p.Description,
        p.Brand,
        p.Sku,
        p.DiscountPercentage,
        p.Rating,
        p.Weight,
        p.Dimensions?.Width,
        p.Dimensions?.Height,
        p.Dimensions?.Depth,
        p.WarrantyInformation,
        p.ShippingInformation,
        p.AvailabilityStatus,
        p.ReturnPolicy,
        p.MinimumOrderQuantity,
        p.Meta?.Barcode,
        p.Meta?.QrCode,
        p.Tags,
        p.Images,
        p.Reviews?.Select(r => new ExternalReviewDto(r.Rating, r.Comment, r.ReviewerName, r.ReviewerEmail, r.Date)).ToList());

    private sealed record DummyJsonResponse([property: JsonPropertyName("products")] List<DummyJsonProduct> Products);

    private sealed record DummyJsonProduct(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("price")] decimal Price,
        [property: JsonPropertyName("stock")] int Stock,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("thumbnail")] string? Thumbnail,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("brand")] string? Brand,
        [property: JsonPropertyName("sku")] string? Sku,
        [property: JsonPropertyName("discountPercentage")] decimal? DiscountPercentage,
        [property: JsonPropertyName("rating")] decimal? Rating,
        [property: JsonPropertyName("weight")] decimal? Weight,
        [property: JsonPropertyName("dimensions")] DummyJsonDimensions? Dimensions,
        [property: JsonPropertyName("warrantyInformation")] string? WarrantyInformation,
        [property: JsonPropertyName("shippingInformation")] string? ShippingInformation,
        [property: JsonPropertyName("availabilityStatus")] string? AvailabilityStatus,
        [property: JsonPropertyName("returnPolicy")] string? ReturnPolicy,
        [property: JsonPropertyName("minimumOrderQuantity")] int? MinimumOrderQuantity,
        [property: JsonPropertyName("meta")] DummyJsonMeta? Meta,
        [property: JsonPropertyName("tags")] List<string>? Tags,
        [property: JsonPropertyName("images")] List<string>? Images,
        [property: JsonPropertyName("reviews")] List<DummyJsonReview>? Reviews);

    private sealed record DummyJsonDimensions(
        [property: JsonPropertyName("width")] decimal? Width,
        [property: JsonPropertyName("height")] decimal? Height,
        [property: JsonPropertyName("depth")] decimal? Depth);

    private sealed record DummyJsonMeta(
        [property: JsonPropertyName("barcode")] string? Barcode,
        [property: JsonPropertyName("qrCode")] string? QrCode);

    private sealed record DummyJsonReview(
        [property: JsonPropertyName("rating")] int Rating,
        [property: JsonPropertyName("comment")] string Comment,
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("reviewerName")] string ReviewerName,
        [property: JsonPropertyName("reviewerEmail")] string ReviewerEmail);
}
