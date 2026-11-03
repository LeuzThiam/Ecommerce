using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("ProcessedWebhookEvents");

        // L'Id de l'événement Stripe est directement la clé primaire — c'est exactement ce
        // qui garantit l'idempotence : une deuxième insertion avec le même EventId violerait
        // la contrainte de clé, donc on vérifie son existence avant d'essayer.
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasMaxLength(200);
        builder.Property(e => e.ProcessedAtUtc).IsRequired();
    }
}
