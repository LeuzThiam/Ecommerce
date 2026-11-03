using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CustomerId).IsRequired();
        builder.HasIndex(c => c.CustomerId).IsUnique();

        // Total est une propriété calculée (somme des sous-totaux), pas une vraie colonne —
        // on dit explicitement à EF Core de l'ignorer, sinon il essaiera (et échouera) de la
        // traiter comme une colonne normale.
        builder.Ignore(c => c.Total);

        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey("CartId")
            .OnDelete(DeleteBehavior.Cascade);

        // Items n'a pas de setter public (IReadOnlyList<CartItem>) — on dit à EF Core d'écrire
        // directement dans le champ privé _items plutôt que de chercher un setter qui n'existe
        // pas.
        builder.Navigation(c => c.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
