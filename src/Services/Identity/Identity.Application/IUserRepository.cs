

using Identity.Domain.Identity;

namespace Identity.Application;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken );
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
}