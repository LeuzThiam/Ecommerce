using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

[Authorize]
public sealed class OrdersController(IOrderingApiClient orderingApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await orderingApiClient.GetOrderHistoryAsync(GetAccessToken(), cancellationToken);
        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderingApiClient.GetOrderAsync(GetAccessToken(), id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    public async Task<IActionResult> Invoice(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await orderingApiClient.GetInvoiceAsync(GetAccessToken(), id, cancellationToken);
        if (invoice.Bytes is not null)
        {
            return File(invoice.Bytes, "application/pdf", $"facture-{id}.pdf");
        }

        if (invoice.ErrorMessage is not null)
        {
            TempData["Error"] = invoice.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        return NotFound();
    }

    private string GetAccessToken() => User.FindFirstValue(WebClaimTypes.AccessToken)!;
}
