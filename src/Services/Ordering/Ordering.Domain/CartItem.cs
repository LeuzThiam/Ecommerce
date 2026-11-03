using Ordering.Domain.Common;

namespace Ordering.Domain;

public sealed class CartItem
{
    public Guid ProductId { get; }
    public string ProductName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal Subtotal => UnitPrice * Quantity;

    private CartItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public static CartItem Create(Guid productId, string productName, decimal unitPrice, int quantity)
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

        return new CartItem(productId, productName.Trim(), unitPrice, quantity);
    }

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;

    internal void SetQuantity(int quantity) => Quantity = quantity;
}
