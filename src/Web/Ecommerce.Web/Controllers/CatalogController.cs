using Ecommerce.Web.Clients;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public sealed class CatalogController(ICatalogApiClient catalogApiClient) : Controller
{
    public async Task<IActionResult> Index(string? query, Guid? categoryId, int page, CancellationToken cancellationToken)
    {
        var products = await catalogApiClient.SearchProductsAsync(query, categoryId, page <= 0 ? 1 : page, cancellationToken);
        ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        ViewBag.Query = query;
        ViewBag.CategoryId = categoryId;

        return View(products);
    }

    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        return View(categories);
    }

    [HttpGet("Catalog/Product/{slug}")]
    public async Task<IActionResult> Product(string slug, CancellationToken cancellationToken)
    {
        var product = await catalogApiClient.GetProductBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }
}
