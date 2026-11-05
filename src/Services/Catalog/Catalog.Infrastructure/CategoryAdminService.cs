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

    public async Task<CategoryDto?> UpdateAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        category.Update(name);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }
}
