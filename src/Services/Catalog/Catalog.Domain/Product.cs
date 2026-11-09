using System.Globalization;
using System.Text;
using Catalog.Domain.Common;

namespace Catalog.Domain;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public string? ImageUrl { get; private set; }
    public IReadOnlyList<ProductVariant> Variants => _variants;

    private Product(Guid id, string name, string slug, decimal price, int stock, Guid categoryId, bool isActive, string? imageUrl)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        IsActive = isActive;
        ImageUrl = imageUrl;
    }

    public static Product Create(string name, decimal price, int stock, Guid categoryId, string? imageUrl = null)
    {
        Validate(name, price, stock);

        return new Product(Guid.NewGuid(), name.Trim(), GenerateSlug(name), price, stock, categoryId, isActive: true, imageUrl);
    }

    public void Update(string name, decimal price, int stock, Guid categoryId, string? imageUrl = null)
    {
        Validate(name, price, stock);

        Name = name.Trim();
        Slug = GenerateSlug(name);
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        ImageUrl = imageUrl;
    }

    // Désactivation logique seulement - jamais de suppression physique. Un produit déjà commandé
    // doit rester consultable dans l'historique des commandes (OrderItem conserve de toute façon
    // sa propre copie du nom/prix au moment de l'achat, indépendante de ce Product).
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void Validate(string name, decimal price, int stock)
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

    public ProductVariant AddVariant(string name, decimal price, int stock)
    {
        var variant = ProductVariant.Create(name, price, stock);
        _variants.Add(variant);
        return variant;
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