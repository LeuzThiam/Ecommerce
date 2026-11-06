using Ordering.Domain.Common;

namespace Ordering.Domain;

public sealed class Payment
{
    public Guid Id { get; }
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public PaymentStatus Status { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public DateTime CreatedAtUtc { get; }

    private Payment(Guid id, Guid orderId, decimal amount, DateTime createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        Amount = amount;
        CreatedAtUtc = createdAtUtc;
        Status = PaymentStatus.Pending;
    }

    public static Payment Create(Guid orderId, decimal amount)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("L'identifiant de la commande est requis.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Le montant doit être positif.");
        }

        return new Payment(Guid.NewGuid(), orderId, amount, DateTime.UtcNow);
    }

    public void AttachProviderPaymentId(string providerPaymentId)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new DomainException("L'identifiant du paiement chez le fournisseur est requis.");
        }

        ProviderPaymentId = providerPaymentId;
    }

    public void MarkAsSucceeded()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Seul un paiement en attente peut être confirmé.");
        }

        Status = PaymentStatus.Succeeded;
    }

    public void MarkAsFailed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Seul un paiement en attente peut échouer.");
        }

        Status = PaymentStatus.Failed;
    }

    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
        {
            throw new DomainException("Seul un paiement réussi peut être remboursé.");
        }

        Status = PaymentStatus.Refunded;
    }
}
