namespace Ordering.Application;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(Guid customerId, CancellationToken cancellationToken);
}
