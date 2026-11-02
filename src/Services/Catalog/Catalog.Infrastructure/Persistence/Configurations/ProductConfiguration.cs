using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
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

        // Jeton de concurrence optimiste géré par l'application (pas par IsRowVersion(), qui
        // dépend d'une fonctionnalité propre à SQL Server et ne fonctionne pas sur SQLite).
        // "Version" est une "shadow property" : elle vit uniquement dans la base et le modèle
        // EF Core, jamais dans la classe Product — ça évite de polluer le Domain avec un détail
        // purement technique de persistance. Incrémentée dans ApplicationDbContext.SaveChanges.
        builder.Property<int>("Version").IsConcurrencyToken();
    }
}
