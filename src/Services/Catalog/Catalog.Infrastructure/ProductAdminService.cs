using Catalog.Application;
using Catalog.Domain;
using Catalog.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

public sealed class ProductAdminService(ApplicationDbContext dbContext) : IProductAdminService
{
    public async Task<ProductDto> CreateAsync(string name, decimal price, int stock, Guid categoryId, CancellationToken cancellationToken)
    {
        var product = Product.Create(name, price, stock, categoryId);
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.Slug, product.Price, product.Stock, product.CategoryId);
    }
}
