using Ordering.Application;

namespace Ordering.Api.Tests;

// Remplace StripePaymentGateway dans les tests. Les webhooks de test utilisent un format texte
// simple ("eventId|type|providerPaymentId") plutôt que du vrai JSON Stripe — ce n'est pas le SDK
// Stripe qu'on teste ici, mais notre propre logique (idempotence, machine à états).
public sealed class FakePaymentGateway : IPaymentGateway
{
    private int _counter;

    public const string ValidSignature = "valid-signature";

    public Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, CancellationToken cancellationToken)
    {
        var id = $"pi_fake_{++_counter}";
        return Task.FromResult(new PaymentIntentResult(id, $"{id}_secret"));
    }

    public WebhookEvent? ParseWebhookEvent(string payload, string signatureHeader)
    {
        if (signatureHeader != ValidSignature)
        {
            return null;
        }

        var parts = payload.Split('|');
        return new WebhookEvent(parts[0], parts[1], parts[2]);
    }
}
