using Identity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .IsRequired()
            .HasMaxLength(320);

        builder.HasMany(u => u.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Addresses n'a pas de setter public (IReadOnlyList<Address>) — on dit à EF Core
        // d'écrire directement dans le champ privé _addresses plutôt que de chercher un setter
        // qui n'existe pas.
        builder.Navigation(u => u.Addresses).HasField("_addresses").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}