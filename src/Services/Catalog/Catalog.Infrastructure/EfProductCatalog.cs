using Catalog.Application;
using Catalog.Application.Common;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class EfProductCatalog(ApplicationDbContext dbContext) : IProductCatalog
{
    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearch search, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();

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
            _ => query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Slug, p.Price, p.Stock, p.CategoryId))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, search.Page, search.PageSize, totalCount);
    }

    public async Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => new ProductDto(p.Id, p.Name, p.Slug, p.Price, p.Stock, p.CategoryId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
