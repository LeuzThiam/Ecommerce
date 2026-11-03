using Microsoft.Extensions.Options;
using Ordering.Application;
using Stripe;

namespace Ordering.Infrastructure;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException(
                "Stripe:SecretKey n'est pas configuré — impossible de créer un paiement réel sans clé de test Stripe.");
        }

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(new PaymentIntentCreateOptions
        {
            // Stripe exprime les montants en plus petite unité monétaire (centimes pour l'euro
            // ou le dollar), jamais en décimal — d'où la conversion.
            Amount = (long)(amount * 100),
            Currency = currency,
        }, cancellationToken: cancellationToken);

        return new PaymentIntentResult(intent.Id, intent.ClientSecret);
    }

    public WebhookEvent? ParseWebhookEvent(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return null;
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);

            if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            {
                return null;
            }

            return new WebhookEvent(stripeEvent.Id, stripeEvent.Type, paymentIntent.Id);
        }
        catch (StripeException)
        {
            // Signature invalide ou payload altéré — on ne fait jamais confiance à un webhook
            // qui ne passe pas cette vérification cryptographique.
            return null;
        }
    }
}
