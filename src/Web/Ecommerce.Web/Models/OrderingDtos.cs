namespace Ecommerce.Web.Models;

// Même principe que CatalogDtos.cs : copie locale de la forme des réponses d'Ordering.Api.
public sealed record CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record CartDto(Guid Id, Guid CustomerId, IReadOnlyList<CartItemDto> Items, decimal Total);

public sealed record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record OrderDto(Guid Id, Guid CustomerId, string Status, IReadOnlyList<OrderItemDto> Items, decimal Total);
