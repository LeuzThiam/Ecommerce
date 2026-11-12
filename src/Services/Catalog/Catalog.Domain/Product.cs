using System.Globalization;
using System.Text;
using Catalog.Domain.Common;

namespace Catalog.Domain;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductReview> _reviews = [];
    // Non-readonly, contrairement à _variants/_reviews : SetExternalDetails réassigne entièrement
    // la liste (plutôt que Clear()+AddRange() en place) pour qu'EF Core détecte le changement par
    // différence de référence, sans avoir besoin d'un ValueComparer structurel sur la colonne JSON.
    private List<string> _tags = [];
    private List<string> _images = [];

    public Guid Id { get; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public IReadOnlyList<ProductVariant> Variants => _variants;

    // Attributs "riches" optionnels, alimentés uniquement par l'import dummyjson.com (voir
    // ProductExternalDetails) - jamais saisis par un admin, donc toujours nullable/vides par
    // défaut pour un produit créé à la main.
    public string? Description { get; private set; }
    public string? Brand { get; private set; }
    public string? Sku { get; private set; }
    public decimal? DiscountPercentage { get; private set; }
    public decimal? Rating { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? DimensionWidth { get; private set; }
    public decimal? DimensionHeight { get; private set; }
    public decimal? DimensionDepth { get; private set; }
    public string? WarrantyInformation { get; private set; }
    public string? ShippingInformation { get; private set; }
    public string? AvailabilityStatus { get; private set; }
    public string? ReturnPolicy { get; private set; }
    public int? MinimumOrderQuantity { get; private set; }
    public string? Barcode { get; private set; }
    public string? QrCodeUrl { get; private set; }
    public IReadOnlyList<string> Tags => _tags;
    public IReadOnlyList<string> Images => _images;
    public IReadOnlyList<ProductReview> Reviews => _reviews;

    private Product(Guid id, string name, string slug, decimal price, int stock, Guid categoryId, bool isActive, string? imageUrl, DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        IsActive = isActive;
        ImageUrl = imageUrl;
        CreatedAtUtc = createdAtUtc;
    }

    public static Product Create(string name, decimal price, int stock, Guid categoryId, string? imageUrl = null)
    {
        Validate(name, price, stock);

        return new Product(Guid.NewGuid(), name.Trim(), GenerateSlug(name), price, stock, categoryId, isActive: true, imageUrl, DateTime.UtcNow);
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

    public void SetExternalDetails(ProductExternalDetails details)
    {
        // On ne remplace l'image que si la source en fournit une : un ré-import ne doit jamais
        // effacer une image choisie à la main par un admin juste parce que dummyjson n'en a pas.
        if (details.ImageUrl is not null)
        {
            ImageUrl = details.ImageUrl;
        }

        Description = details.Description;
        Brand = details.Brand;
        Sku = details.Sku;
        DiscountPercentage = details.DiscountPercentage;
        Rating = details.Rating;
        Weight = details.Weight;
        DimensionWidth = details.DimensionWidth;
        DimensionHeight = details.DimensionHeight;
        DimensionDepth = details.DimensionDepth;
        WarrantyInformation = details.WarrantyInformation;
        ShippingInformation = details.ShippingInformation;
        AvailabilityStatus = details.AvailabilityStatus;
        ReturnPolicy = details.ReturnPolicy;
        MinimumOrderQuantity = details.MinimumOrderQuantity;
        Barcode = details.Barcode;
        QrCodeUrl = details.QrCodeUrl;

        _tags = details.Tags?.ToList() ?? [];
        _images = details.Images?.ToList() ?? [];
    }

    // Remplace entièrement les avis plutôt que d'accumuler - un ré-import ne doit pas dupliquer
    // les mêmes avis dummyjson à chaque exécution.
    public void ReplaceReviews(IEnumerable<(int Rating, string Comment, string ReviewerName, string ReviewerEmail, DateTime ReviewedAtUtc)> reviews)
    {
        _reviews.Clear();
        foreach (var review in reviews)
        {
            _reviews.Add(ProductReview.Create(review.Rating, review.Comment, review.ReviewerName, review.ReviewerEmail, review.ReviewedAtUtc));
        }
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