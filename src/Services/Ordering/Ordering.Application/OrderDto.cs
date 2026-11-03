namespace Ordering.Application;

public sealed record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record OrderDto(Guid Id, Guid CustomerId, string Status, IReadOnlyList<OrderItemDto> Items, decimal Total);
