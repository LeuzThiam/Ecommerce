using Identity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);

        // Id est généré par le domaine (Guid.NewGuid() dans Address.Create), jamais par EF/la
        // base. Sans ça, une adresse ajoutée à un User existant (chargé via une requête, donc
        // déjà "Unchanged") est détectée comme Modified plutôt qu'Added lors de SaveChanges —
        // EF suppose par convention qu'une clé Guid non-vide signale une ligne déjà en base,
        // ce qui génère un UPDATE sur une ligne qui n'existe pas encore (0 ligne affectée).
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Line1).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Country).IsRequired().HasMaxLength(100);
        builder.Property(a => a.IsDefault).IsRequired();
    }
}
