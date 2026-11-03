using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        // CartItem n'a pas d'Id propre au Domain (il est identifié par la combinaison
        // panier + produit) — "CartId" est la clé étrangère ajoutée par CartConfiguration,
        // invisible depuis Cart/CartItem en C#, gérée uniquement par EF Core.
        builder.HasKey("CartId", "ProductId");

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.AddedAtUtc).IsRequired();

        // Subtotal est calculé (UnitPrice * Quantity), pas une colonne.
        builder.Ignore(i => i.Subtotal);
    }
}
