using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class OrdersController(IOrderingApiClient orderingApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await orderingApiClient.GetAllOrdersAdminAsync(GetAccessToken(), cancellationToken);
        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderingApiClient.GetOrderAdminAsync(GetAccessToken(), id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await orderingApiClient.RefundOrderAsync(GetAccessToken(), id, cancellationToken);
            TempData["Success"] = "Commande remboursée.";
        }
        catch (OrderingApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private string GetAccessToken() => User.FindFirstValue(WebClaimTypes.AccessToken)!;
}
