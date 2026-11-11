using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Areas.Admin.Controllers;

// Tableau de bord admin : uniquement des compteurs calculés à partir de données déjà exposées par
// Catalog.Api/Ordering.Api (recherche admin, liste des catégories, liste des commandes) - aucune
// nouvelle règle métier, aucun nouvel endpoint.
[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class OverviewController(ICatalogApiClient catalogApiClient, IOrderingApiClient orderingApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accessToken = User.FindFirstValue(WebClaimTypes.AccessToken)!;

        var products = await catalogApiClient.SearchAllProductsAsync(accessToken, null, null, 1, cancellationToken);
        var categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        var orders = await orderingApiClient.GetAllOrdersAdminAsync(accessToken, cancellationToken);

        ViewBag.ProductCount = products.TotalCount;
        ViewBag.CategoryCount = categories.Count;
        ViewBag.OrderCount = orders.Count;
        ViewBag.PendingCount = orders.Count(o => o.Status == "PendingPayment");
        ViewBag.RecentOrders = orders.OrderByDescending(o => o.CreatedAtUtc).Take(5).ToList();

        return View();
    }
}
