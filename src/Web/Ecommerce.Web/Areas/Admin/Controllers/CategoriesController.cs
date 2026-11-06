using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class CategoriesController(ICatalogApiClient catalogApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, CancellationToken cancellationToken)
    {
        try
        {
            await catalogApiClient.CreateCategoryAsync(GetAccessToken(), name, cancellationToken);
            TempData["Success"] = "Catégorie créée.";
        }
        catch (CatalogApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var categories = await catalogApiClient.GetCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(c => c.Id == id);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, CancellationToken cancellationToken)
    {
        try
        {
            var category = await catalogApiClient.UpdateCategoryAsync(GetAccessToken(), id, name, cancellationToken);
            if (category is null)
            {
                return NotFound();
            }

            TempData["Success"] = "Catégorie mise à jour.";
            return RedirectToAction(nameof(Index));
        }
        catch (CatalogApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(new Models.CategoryDto(id, name));
        }
    }

    private string GetAccessToken() => User.FindFirstValue(WebClaimTypes.AccessToken)!;
}
