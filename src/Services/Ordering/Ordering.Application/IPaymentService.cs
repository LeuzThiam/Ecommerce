namespace Ordering.Application;

public sealed record PaymentIntentDto(string ClientSecret);

public interface IPaymentService
{
    Task<PaymentIntentDto> CreatePaymentIntentAsync(Guid orderId, CancellationToken cancellationToken);
    Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken);
}
