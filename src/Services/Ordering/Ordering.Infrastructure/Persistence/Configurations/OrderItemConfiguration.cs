using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey("OrderId", "ProductId");

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Quantity).IsRequired();

        builder.Ignore(i => i.Subtotal);
    }
}
