namespace Ordering.Application;

public interface ICartService
{
    Task<CartDto> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task<CartDto> AddItemAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken);
    Task<CartDto> UpdateQuantityAsync(Guid customerId, Guid productId, int quantity, CancellationToken cancellationToken);
    Task<CartDto> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken);
}
