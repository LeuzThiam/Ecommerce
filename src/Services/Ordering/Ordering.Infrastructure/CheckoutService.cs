using Ordering.Application;
using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Infrastructure;

public sealed class CheckoutService(
    ICartRepository cartRepository,
    IOrderRepository orderRepository,
    IProductCatalogClient catalogClient,
    IStockReservationClient stockClient) : IOrderService
{
    public async Task<OrderDto> CheckoutAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            throw new DomainException("Le panier est vide.");
        }

        var reservedItems = new List<(Guid ProductId, int Quantity)>();
        var orderItems = new List<OrderItem>();

        try
        {
            foreach (var cartItem in cart.Items)
            {
                // On revalide toujours le produit auprès de Catalog.Api au moment du checkout —
                // jamais via le prix/nom déjà stockés dans le panier, qui peuvent être périmés
                // (prix changé, produit retiré, rupture de stock survenue depuis l'ajout).
                var product = await catalogClient.GetProductAsync(cartItem.ProductId, cancellationToken)
                    ?? throw new DomainException($"Le produit \"{cartItem.ProductName}\" n'existe plus.");

                var reserved = await stockClient.TryReserveAsync(product.Id, cartItem.Quantity, cancellationToken);
                if (!reserved)
                {
                    throw new DomainException($"Le produit \"{product.Name}\" n'a plus assez de stock disponible.");
                }

                reservedItems.Add((product.Id, cartItem.Quantity));
                orderItems.Add(OrderItem.Create(product.Id, product.Name, product.Price, cartItem.Quantity));
            }

            var order = Order.Create(customerId, orderItems);
            await orderRepository.AddAsync(order, cancellationToken);
            await orderRepository.SaveChangesAsync(cancellationToken);

            cart.Clear();
            await cartRepository.SaveChangesAsync(cancellationToken);

            return ToDto(order);
        }
        catch
        {
            // Si on a réservé du stock pour les 2 premiers articles du panier puis que le 3e
            // échoue, les réservations des 2 premiers resteraient bloquées indéfiniment sans
            // ça — aucune commande ne sera jamais créée pour les récupérer. On les libère donc
            // explicitement avant de relancer l'erreur.
            foreach (var (productId, quantity) in reservedItems)
            {
                await stockClient.ReleaseAsync(productId, quantity, cancellationToken);
            }

            throw;
        }
    }

    private static OrderDto ToDto(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal))
            .ToList();

        return new OrderDto(order.Id, order.CustomerId, order.Status.ToString(), items, order.Total);
    }
}
