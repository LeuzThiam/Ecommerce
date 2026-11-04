using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public sealed class CheckoutController(IOrderingApiClient orderingApiClient) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var customerId = CustomerCookie.GetOrCreate(HttpContext);
        var order = await orderingApiClient.CheckoutAsync(customerId, cancellationToken);
        if (order is null)
        {
            return RedirectToAction(nameof(CartController.Index), "Cart");
        }

        return RedirectToAction(nameof(Confirmation), new { orderId = order.Id });
    }

    public IActionResult Confirmation(Guid orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }
}
