using Ordering.Domain.Common;

namespace Ordering.Domain;

public sealed class Order
{
    public Guid Id { get; }
    public Guid CustomerId { get; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items;

    public decimal Total => _items.Sum(item => item.Subtotal);

    private Order(Guid id, Guid customerId, DateTime createdAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        CreatedAtUtc = createdAtUtc;
        Status = OrderStatus.PendingPayment;
    }

    public static Order Create(Guid customerId, IReadOnlyList<OrderItem> items)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("L'identifiant du client est requis.");
        }

        if (items.Count == 0)
        {
            throw new DomainException("La commande doit contenir au moins un article.");
        }

        var order = new Order(Guid.NewGuid(), customerId, DateTime.UtcNow);
        order._items.AddRange(items);
        return order;
    }

    public void MarkAsPaid()
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new DomainException("Seule une commande en attente de paiement peut être marquée payée.");
        }

        Status = OrderStatus.Paid;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new DomainException("Seule une commande en attente de paiement peut être annulée.");
        }

        Status = OrderStatus.Cancelled;
    }
}
