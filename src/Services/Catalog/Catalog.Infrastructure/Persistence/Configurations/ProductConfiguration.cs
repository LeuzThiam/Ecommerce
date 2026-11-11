using System.Text.Json;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(2000);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        // Attributs riches optionnels (voir ProductExternalDetails / Product.SetExternalDetails) -
        // alimentés uniquement par l'import dummyjson.com, jamais par un admin.
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Brand).HasMaxLength(200);
        builder.Property(p => p.Sku).HasMaxLength(100);
        builder.Property(p => p.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(p => p.Rating).HasPrecision(3, 2);
        builder.Property(p => p.Weight).HasPrecision(10, 2);
        builder.Property(p => p.DimensionWidth).HasPrecision(10, 2);
        builder.Property(p => p.DimensionHeight).HasPrecision(10, 2);
        builder.Property(p => p.DimensionDepth).HasPrecision(10, 2);
        builder.Property(p => p.WarrantyInformation).HasMaxLength(500);
        builder.Property(p => p.ShippingInformation).HasMaxLength(500);
        builder.Property(p => p.AvailabilityStatus).HasMaxLength(100);
        builder.Property(p => p.ReturnPolicy).HasMaxLength(500);
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.QrCodeUrl).HasMaxLength(2000);

        // Tags/Images : listes de chaînes simples, stockées comme une seule colonne JSON plutôt
        // qu'une table enfant (contrairement à Variants/Reviews) - pas d'identité propre, pas de
        // raison de les gérer comme des entités. Le ValueComparer est nécessaire : sans lui, EF
        // Core ne peut pas détecter qu'une liste a changé (comparaison par référence par défaut).
        var stringListComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        builder.Property(p => p.Tags)
            .HasField("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(p => p.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(stringListComparer);

        // Jeton de concurrence optimiste géré par l'application (pas par IsRowVersion(), qui
        // dépend d'une fonctionnalité propre à SQL Server et ne fonctionne pas sur SQLite).
        // "Version" est une "shadow property" : elle vit uniquement dans la base et le modèle
        // EF Core, jamais dans la classe Product — ça évite de polluer le Domain avec un détail
        // purement technique de persistance. Incrémentée dans ApplicationDbContext.SaveChanges.
        builder.Property<int>("Version").IsConcurrencyToken();

        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey("ProductId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Variants n'a pas de setter public (IReadOnlyList<ProductVariant>) — on dit à EF Core
        // d'écrire directement dans le champ privé _variants plutôt que de chercher un setter
        // qui n'existe pas.
        builder.Navigation(p => p.Variants).HasField("_variants").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Reviews)
            .WithOne()
            .HasForeignKey("ProductId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Reviews).HasField("_reviews").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
