using Identity.Application;
using Identity.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.DomainUsers.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        Email normalizedEmail = Email.Create(email);
        return await dbContext.DomainUsers.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.DomainUsers
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task UpdateAsync(User user, CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}