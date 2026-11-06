using Ordering.Domain;

namespace Ordering.Application;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<bool> HasProcessedEventAsync(string eventId, CancellationToken cancellationToken);
    Task MarkEventAsProcessedAsync(string eventId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
