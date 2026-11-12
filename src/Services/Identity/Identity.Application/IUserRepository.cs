

using Identity.Domain.Identity;

namespace Identity.Application;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken );
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);
}