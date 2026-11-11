using Catalog.Application;
using Catalog.Domain;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class ProductAdminService(ApplicationDbContext dbContext) : IProductAdminService
{
    public async Task<ProductDto> CreateAsync(string name, decimal price, int stock, Guid categoryId, string? imageUrl, CancellationToken cancellationToken)
    {
        var product = Product.Create(name, price, stock, categoryId, imageUrl);
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, string name, decimal price, int stock, Guid categoryId, string? imageUrl, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Update(name, price, stock, categoryId, imageUrl);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Activate();
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> AddVariantAsync(Guid productId, string name, decimal price, int stock, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.AddVariant(name, price, stock);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> SetExternalDetailsAsync(Guid id, ExternalProductDto details, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.Variants)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.SetExternalDetails(new ProductExternalDetails(
            details.ImageUrl,
            details.Description,
            details.Brand,
            details.Sku,
            details.DiscountPercentage,
            details.Rating,
            details.Weight,
            details.DimensionWidth,
            details.DimensionHeight,
            details.DimensionDepth,
            details.WarrantyInformation,
            details.ShippingInformation,
            details.AvailabilityStatus,
            details.ReturnPolicy,
            details.MinimumOrderQuantity,
            details.Barcode,
            details.QrCodeUrl,
            details.Tags,
            details.Images));

        if (details.Reviews is not null)
        {
            product.ReplaceReviews(details.Reviews.Select(r => (r.Rating, r.Comment, r.ReviewerName, r.ReviewerEmail, r.ReviewedAtUtc)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(product);
    }

    private static ProductDto ToDto(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Slug,
            product.Price,
            product.Stock,
            product.CategoryId,
            product.IsActive,
            product.ImageUrl,
            product.CreatedAtUtc,
            product.Description,
            product.Brand,
            product.Sku,
            product.DiscountPercentage,
            product.Rating,
            product.Weight,
            product.DimensionWidth,
            product.DimensionHeight,
            product.DimensionDepth,
            product.WarrantyInformation,
            product.ShippingInformation,
            product.AvailabilityStatus,
            product.ReturnPolicy,
            product.MinimumOrderQuantity,
            product.Barcode,
            product.QrCodeUrl,
            product.Tags,
            product.Images,
            product.Reviews.Select(r => new ProductReviewDto(r.Id, r.Rating, r.Comment, r.ReviewerName, r.ReviewerEmail, r.ReviewedAtUtc)).ToList(),
            product.Variants.Select(v => new ProductVariantDto(v.Id, v.Name, v.Price, v.Stock)).ToList());
}
