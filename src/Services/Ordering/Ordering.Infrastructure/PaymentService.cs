using Ordering.Application;
using Ordering.Domain;
using Ordering.Domain.Common;

namespace Ordering.Infrastructure;

public sealed class PaymentService(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IStockReservationClient stockClient) : IPaymentService
{
    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new DomainException("Cette commande n'existe pas.");

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new DomainException("Cette commande n'est plus en attente de paiement.");
        }

        var payment = Payment.Create(order.Id, order.Total);
        var intent = await paymentGateway.CreatePaymentIntentAsync(order.Total, "cad", cancellationToken);
        payment.AttachProviderPaymentId(intent.ProviderPaymentId);

        await paymentRepository.AddAsync(payment, cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        return new PaymentIntentDto(intent.ClientSecret);
    }

    public async Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken)
    {
        // ParseWebhookEvent retourne null si la signature est invalide ou absente — dans ce
        // cas on ne touche à rien du tout. Aucune commande n'est jamais confirmée sans un
        // webhook réellement signé par Stripe.
        var webhookEvent = paymentGateway.ParseWebhookEvent(payload, signatureHeader);
        if (webhookEvent is null)
        {
            return;
        }

        // Idempotence : si on a déjà traité cet événement (Stripe garantit "au moins une fois",
        // donc des doublons arrivent), on s'arrête ici sans rien refaire.
        if (await paymentRepository.HasProcessedEventAsync(webhookEvent.EventId, cancellationToken))
        {
            return;
        }

        var payment = await paymentRepository.GetByProviderPaymentIdAsync(webhookEvent.ProviderPaymentId, cancellationToken);
        if (payment is null)
        {
            await paymentRepository.MarkEventAsProcessedAsync(webhookEvent.EventId, cancellationToken);
            await paymentRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var order = await orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);

        switch (webhookEvent.Type)
        {
            case "payment_intent.succeeded":
                payment.MarkAsSucceeded();
                order?.MarkAsPaid();
                break;

            case "payment_intent.payment_failed":
                payment.MarkAsFailed();
                if (order is not null)
                {
                    order.Cancel();
                    foreach (var item in order.Items)
                    {
                        await stockClient.ReleaseAsync(item.ProductId, item.Quantity, cancellationToken);
                    }
                }
                break;
        }

        await paymentRepository.MarkEventAsProcessedAsync(webhookEvent.EventId, cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
