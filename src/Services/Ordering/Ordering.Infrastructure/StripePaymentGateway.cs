using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Application;
using Stripe;

namespace Ordering.Infrastructure;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(IOptions<StripeOptions> options, ILogger<StripePaymentGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
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
            // throwOnApiVersionMismatch: false - Stripe.net valide par défaut que l'évènement a été
            // émis avec la même "version d'API Stripe" que celle figée dans ce package NuGet, et
            // lève sinon une exception. Notre compte Stripe (et la CLI utilisée pour tester les
            // webhooks en local) tourne sur une version d'API plus récente que Stripe.net 53.1.0 -
            // la signature elle-même reste vérifiée normalement, seule cette vérification annexe
            // est désactivée (recommandation officielle du message d'erreur Stripe).
            var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret, throwOnApiVersionMismatch: false);

            if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            {
                return null;
            }

            return new WebhookEvent(stripeEvent.Id, stripeEvent.Type, paymentIntent.Id);
        }
        catch (StripeException ex)
        {
            // Signature invalide ou payload altéré — on ne fait jamais confiance à un webhook
            // qui ne passe pas cette vérification cryptographique.
            _logger.LogWarning(ex, "Echec de vérification de la signature du webhook Stripe.");
            return null;
        }
    }

    public async Task<RefundResult> RefundAsync(string providerPaymentId, decimal amount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException(
                "Stripe:SecretKey n'est pas configuré — impossible de rembourser sans clé de test Stripe.");
        }

        var service = new RefundService();
        var refund = await service.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = providerPaymentId,
            Amount = (long)(amount * 100),
        }, cancellationToken: cancellationToken);

        return new RefundResult(refund.Id);
    }
}
