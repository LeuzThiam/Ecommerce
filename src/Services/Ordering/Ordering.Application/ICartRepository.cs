using Ordering.Domain;

namespace Ordering.Application;

public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task AddAsync(Cart cart, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
