using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public sealed class CartController(IOrderingApiClient orderingApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customerId = CustomerCookie.GetOrCreate(HttpContext);
        var cart = await orderingApiClient.GetCartAsync(customerId, cancellationToken);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var customerId = CustomerCookie.GetOrCreate(HttpContext);
        await orderingApiClient.AddItemAsync(customerId, productId, quantity <= 0 ? 1 : quantity, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var customerId = CustomerCookie.GetOrCreate(HttpContext);
        await orderingApiClient.UpdateItemQuantityAsync(customerId, productId, quantity, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken cancellationToken)
    {
        var customerId = CustomerCookie.GetOrCreate(HttpContext);
        await orderingApiClient.RemoveItemAsync(customerId, productId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
