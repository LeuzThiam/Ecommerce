using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public sealed class CheckoutController(IOrderingApiClient orderingApiClient, IConfiguration configuration) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var customerId = CustomerCookie.GetEffectiveCustomerId(HttpContext);
        var order = await orderingApiClient.CheckoutAsync(customerId, cancellationToken);
        if (order is null)
        {
            return RedirectToAction(nameof(CartController.Index), "Cart");
        }

        return RedirectToAction(nameof(Payment), new { orderId = order.Id });
    }

    // Etape de paiement : demande un PaymentIntent Stripe pour cette commande et affiche le
    // formulaire de carte (Stripe Elements) - la commande reste PendingPayment tant que Stripe
    // n'a pas confirmé le paiement via webhook.
    public async Task<IActionResult> Payment(Guid orderId, CancellationToken cancellationToken)
    {
        var publishableKey = configuration["Stripe:PublishableKey"];
        if (string.IsNullOrWhiteSpace(publishableKey))
        {
            // Clé publique Stripe absente de la config - on ne peut pas afficher le formulaire de
            // carte. On le signale clairement plutôt que de laisser Stripe.js échouer en silence
            // côté navigateur.
            ViewBag.ConfigurationError = "Le paiement n'est pas configuré sur cet environnement (Stripe:PublishableKey manquante).";
            ViewBag.OrderId = orderId;
            return View();
        }

        var intent = await orderingApiClient.CreatePaymentIntentAsync(orderId, cancellationToken);
        ViewBag.OrderId = orderId;
        ViewBag.ClientSecret = intent.ClientSecret;
        ViewBag.PublishableKey = publishableKey;
        return View();
    }

    public IActionResult Confirmation(Guid orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }
}
