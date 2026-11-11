using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.ViewComponents;

// Rendu dans le navbar sur chaque page - affiche le nombre d'articles du panier courant pour
// que le visiteur n'ait pas besoin d'ouvrir /Cart pour savoir s'il a déjà quelque chose dedans.
public sealed class CartSummaryViewComponent(IOrderingApiClient orderingApiClient) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customerId = CustomerCookie.GetEffectiveCustomerId(HttpContext);
        var cart = await orderingApiClient.GetCartAsync(customerId, HttpContext.RequestAborted);
        var itemCount = cart.Items.Sum(i => i.Quantity);

        return View(itemCount);
    }
}
