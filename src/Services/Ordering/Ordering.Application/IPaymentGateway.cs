namespace Ordering.Application;

// Abstraction au-dessus de Stripe — l'Application ne connaît que ce contrat, jamais le SDK
// Stripe directement. Permet de tester toute notre logique (idempotence, machine à états) avec
// un faux fournisseur, sans jamais appeler le vrai Stripe pendant les tests.
public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, CancellationToken cancellationToken);

    // Vérifie la signature du webhook et retourne l'événement s'il est valide, null sinon.
    // C'est la seule façon d'obtenir un WebhookEvent — impossible de forger une confirmation
    // de paiement sans passer la vérification de signature.
    WebhookEvent? ParseWebhookEvent(string payload, string signatureHeader);
}

public sealed record PaymentIntentResult(string ProviderPaymentId, string ClientSecret);

public sealed record WebhookEvent(string EventId, string Type, string ProviderPaymentId);
