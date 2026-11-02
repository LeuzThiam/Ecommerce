using Catalog.Application;
using Catalog.Domain;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class CategoryAdminService(ApplicationDbContext dbContext) : ICategoryAdminService
{
    public async Task<CategoryDto> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var category = Category.Create(name);
        await dbContext.Categories.AddAsync(category, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);
    }
}
