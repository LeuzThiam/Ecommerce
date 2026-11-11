using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public sealed class UsersController(IIdentityApiClient identityApiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await identityApiClient.GetAllUsersAdminAsync(GetAccessToken(), cancellationToken);
        ViewBag.CurrentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await identityApiClient.PromoteUserAsync(GetAccessToken(), id, cancellationToken);
            TempData["Success"] = "Utilisateur promu administrateur.";
        }
        catch (IdentityApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Demote(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await identityApiClient.DemoteUserAsync(GetAccessToken(), id, cancellationToken);
            TempData["Success"] = "Droits administrateur retirés.";
        }
        catch (IdentityApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private string GetAccessToken() => User.FindFirstValue(WebClaimTypes.AccessToken)!;
}
