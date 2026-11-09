using System.Diagnostics;
using Ecommerce.Web.Clients;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Web.Models;

namespace Ecommerce.Web.Controllers;

public class HomeController(ICatalogApiClient catalogApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var featured = await catalogApiClient.SearchProductsAsync(null, null, 1, cancellationToken);
        ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);

        return View(featured);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
