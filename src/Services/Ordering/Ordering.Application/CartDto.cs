namespace Ordering.Application;

public sealed record CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record CartDto(Guid Id, Guid CustomerId, IReadOnlyList<CartItemDto> Items, decimal Total);
