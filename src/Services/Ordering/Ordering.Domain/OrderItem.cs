using Ordering.Domain.Common;

namespace Ordering.Domain;

public sealed class OrderItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }

    public decimal Subtotal => UnitPrice * Quantity;

    private OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public static OrderItem Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("Le nom du produit est requis.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Le prix ne peut pas être négatif.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("La quantité doit être positive.");
        }

        return new OrderItem(productId, productName.Trim(), unitPrice, quantity);
    }
}
