using System.Globalization;
using System.Text;
using Catalog.Domain.Common;

namespace Catalog.Domain;

public sealed class Product
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; }

    private Product(Guid id, string name, string slug, decimal price, int stock, Guid categoryId)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
    }

    public static Product Create(string name, decimal price, int stock, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Le nom du produit est requis.");
        }

        if (price < 0)
        {
            throw new DomainException("Le prix ne peut pas être négatif.");
        }

        if (stock < 0)
        {
            throw new DomainException("Le stock ne peut pas être négatif.");
        }

        return new Product(Guid.NewGuid(), name.Trim(), GenerateSlug(name), price, stock, categoryId);
    }

    public bool TryReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("La quantité doit être positive.");
        }

        if (Stock < quantity)
        {
            return false;
        }

        Stock -= quantity;
        return true;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("La quantité doit être positive.");
        }

        Stock += quantity;
    }

    private static string GenerateSlug(string name)
    {
        // Normalize en FormD sépare chaque lettre accentuée en "lettre de base" + "accent"
        // (ex: 'é' devient 'e' + un accent combinant) — on garde la lettre de base et on
        // ignore l'accent, pour obtenir un slug sans caractères accentués (sûr dans une URL).
        var decomposed = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder();
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }
}