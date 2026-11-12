using System.Linq.Expressions;
using Catalog.Application;
using Catalog.Application.Common;
using Catalog.Domain;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class EfProductCatalog(ApplicationDbContext dbContext) : IProductCatalog
{
    // Projection "détail" partagée par GetBySlugAsync/GetByIdAsync : attributs riches, variantes
    // et avis, consultés uniquement à l'unité - jamais en liste (voir le commentaire sur
    // SearchAsync plus bas).
    private static readonly Expression<Func<Product, ProductDto>> DetailProjection = p => new ProductDto(
        p.Id, p.Name, p.Slug, p.Price, p.Stock, p.CategoryId, p.IsActive, p.ImageUrl, p.CreatedAtUtc,
        p.Description, p.Brand, p.Sku, p.DiscountPercentage, p.Rating, p.Weight,
        p.DimensionWidth, p.DimensionHeight, p.DimensionDepth,
        p.WarrantyInformation, p.ShippingInformation, p.AvailabilityStatus, p.ReturnPolicy,
        p.MinimumOrderQuantity, p.Barcode, p.QrCodeUrl,
        p.Tags, p.Images,
        p.Reviews.Select(r => new ProductReviewDto(r.Id, r.Rating, r.Comment, r.ReviewerName, r.ReviewerEmail, r.ReviewedAtUtc)).ToList(),
        p.Variants.Select(v => new ProductVariantDto(v.Id, v.Name, v.Price, v.Stock)).ToList());

    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearch search, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();
        if (!search.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            query = query.Where(p => p.Name.Contains(search.Query));
        }

        if (search.CategoryId is not null)
        {
            query = query.Where(p => p.CategoryId == search.CategoryId);
        }

        if (search.MinimumPrice is not null)
        {
            query = query.Where(p => p.Price >= search.MinimumPrice);
        }

        if (search.MaximumPrice is not null)
        {
            query = query.Where(p => p.Price <= search.MaximumPrice);
        }

        query = search.Sort switch
        {
            "price" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAtUtc),
            _ => query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Slug, p.Price, p.Stock, p.CategoryId, p.IsActive, p.ImageUrl, p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, search.Page, search.PageSize, totalCount);
    }

    // Les deux lectures ci-dessous excluent aussi les produits désactivés : ils ont disparu du
    // catalogue public, donc plus consultables par slug ni achetables (Ordering.Api appelle
    // GetByIdAsync au checkout - un produit désactivé y échoue avec "n'existe plus", ce qui est
    // le comportement voulu).
    // Les variantes ne sont incluses que pour la fiche produit (consultée à l'unité) - la
    // recherche publique ci-dessus, qui liste des pages entières de produits, reste légère.
    public async Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Slug == slug && p.IsActive)
            .Select(DetailProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().Where(p => p.Id == id);
        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .Select(DetailProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
