using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");
        builder.HasKey(r => r.Id);

        // Même raison que ValueGeneratedNever() sur ProductVariant.Id : l'Id est généré par le
        // Domain (ProductReview.Create), pas par la base.
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.ReviewerName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.ReviewerEmail).IsRequired().HasMaxLength(320);
        builder.Property(r => r.ReviewedAtUtc).IsRequired();
    }
}
