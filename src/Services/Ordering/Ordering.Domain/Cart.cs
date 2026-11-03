using Ordering.Domain.Common;

namespace Ordering.Domain;

public sealed class Cart
{
    public Guid Id { get; }
    public Guid CustomerId { get; }

    private readonly List<CartItem> _items = [];
    public IReadOnlyList<CartItem> Items => _items;

    public decimal Total => _items.Sum(item => item.Subtotal);

    private Cart(Guid id, Guid customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Cart Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("L'identifiant du client est requis.");
        }

        return new Cart(Guid.NewGuid(), customerId);
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            return;
        }

        _items.Add(CartItem.Create(productId, productName, unitPrice, quantity));
    }

    public void UpdateQuantity(Guid productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException("Cet article n'est pas dans le panier.");

        if (quantity <= 0)
        {
            _items.Remove(item);
            return;
        }

        item.SetQuantity(quantity);
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null)
        {
            _items.Remove(item);
        }
    }
}
