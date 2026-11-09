using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Models;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class ProductsController(ICatalogApiClient catalogApiClient) : Controller
{
    public async Task<IActionResult> Index(string? query, Guid? categoryId, int page, CancellationToken cancellationToken)
    {
        var result = await catalogApiClient.SearchAllProductsAsync(GetAccessToken(), query, categoryId, page <= 0 ? 1 : page, cancellationToken);
        ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        ViewBag.Query = query;
        ViewBag.CategoryId = categoryId;
        return View(result);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, decimal price, int stock, Guid categoryId, string? imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            await catalogApiClient.CreateProductAsync(GetAccessToken(), name, price, stock, categoryId, imageUrl, cancellationToken);
            TempData["Success"] = "Produit créé.";
            return RedirectToAction(nameof(Index));
        }
        catch (CatalogApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
            return View();
        }
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var product = await catalogApiClient.GetProductByIdAsync(GetAccessToken(), id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, decimal price, int stock, Guid categoryId, string? imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            var product = await catalogApiClient.UpdateProductAsync(GetAccessToken(), id, name, price, stock, categoryId, imageUrl, cancellationToken);
            if (product is null)
            {
                return NotFound();
            }

            TempData["Success"] = "Produit mis à jour.";
            return RedirectToAction(nameof(Index));
        }
        catch (CatalogApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
            return View(new ProductDto(id, name, string.Empty, price, stock, categoryId, ImageUrl: imageUrl));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await catalogApiClient.DeactivateProductAsync(GetAccessToken(), id, cancellationToken);
        TempData["Success"] = "Produit désactivé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await catalogApiClient.ActivateProductAsync(GetAccessToken(), id, cancellationToken);
        TempData["Success"] = "Produit réactivé.";
        return RedirectToAction(nameof(Index));
    }

    private string GetAccessToken() => User.FindFirstValue(WebClaimTypes.AccessToken)!;
}
