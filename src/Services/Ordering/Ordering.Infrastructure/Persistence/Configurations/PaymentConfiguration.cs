using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.OrderId).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.ProviderPaymentId).HasMaxLength(200);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasIndex(p => p.ProviderPaymentId).IsUnique();
    }
}
