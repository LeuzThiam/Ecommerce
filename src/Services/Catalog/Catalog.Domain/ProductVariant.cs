using Catalog.Domain.Common;

namespace Catalog.Domain;

public sealed class ProductVariant
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    private ProductVariant(Guid id, string name, decimal price, int stock)
    {
        Id = id;
        Name = name;
        Price = price;
        Stock = stock;
    }

    internal static ProductVariant Create(string name, decimal price, int stock)
    {
        Validate(name, price, stock);

        return new ProductVariant(Guid.NewGuid(), name.Trim(), price, stock);
    }

    internal void Update(string name, decimal price, int stock)
    {
        Validate(name, price, stock);

        Name = name.Trim();
        Price = price;
        Stock = stock;
    }

    private static void Validate(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Le nom de la variante est requis.");
        }

        if (price < 0)
        {
            throw new DomainException("Le prix ne peut pas être négatif.");
        }

        if (stock < 0)
        {
            throw new DomainException("Le stock ne peut pas être négatif.");
        }
    }
}
