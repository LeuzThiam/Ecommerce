using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(v => v.Id);

        // Id est généré par le domaine (Guid.NewGuid() dans ProductVariant.Create), jamais par
        // EF/la base — même raison que ValueGeneratedNever() sur Identity.Domain.Address : sans
        // ça, une variante ajoutée à un Product existant (déjà "Unchanged") est détectée comme
        // Modified plutôt qu'Added lors de SaveChanges.
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Price).HasPrecision(18, 2);
        builder.Property(v => v.Stock).IsRequired();
    }
}
