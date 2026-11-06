using Microsoft.EntityFrameworkCore;
using Ordering.Application;
using Ordering.Domain;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public sealed class PaymentRepository(ApplicationDbContext dbContext) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.Payments
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasProcessedEventAsync(string eventId, CancellationToken cancellationToken)
    {
        return await dbContext.ProcessedWebhookEvents.AnyAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task MarkEventAsProcessedAsync(string eventId, CancellationToken cancellationToken)
    {
        await dbContext.ProcessedWebhookEvents.AddAsync(ProcessedWebhookEvent.Create(eventId), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
