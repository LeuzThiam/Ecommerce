using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public sealed class AccountController(IIdentityApiClient identityApiClient) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl, CancellationToken cancellationToken)
    {
        var loginResult = await identityApiClient.LoginAsync(email, password, cancellationToken);
        if (loginResult is null)
        {
            ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(WebClaimTypes.AccessToken, loginResult.AccessToken),
        };
        claims.AddRange(GetRoleClaims(loginResult.AccessToken));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var profile = await identityApiClient.GetProfileAsync(GetAccessToken(), cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string displayName, CancellationToken cancellationToken)
    {
        await identityApiClient.UpdateProfileAsync(GetAccessToken(), displayName, cancellationToken);
        TempData["Success"] = "Profil mis à jour.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public async Task<IActionResult> Addresses(CancellationToken cancellationToken)
    {
        var addresses = await identityApiClient.GetAddressesAsync(GetAccessToken(), cancellationToken);
        return View(addresses);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(
        string line1,
        string? line2,
        string city,
        string postalCode,
        string country,
        CancellationToken cancellationToken)
    {
        await identityApiClient.AddAddressAsync(GetAccessToken(), line1, line2, city, postalCode, country, cancellationToken);
        TempData["Success"] = "Adresse ajoutée.";
        return RedirectToAction(nameof(Addresses));
    }

    private string GetAccessToken() => User.FindFirstValue(WebClaimTypes.AccessToken)!;

    // Le JWT encode les rôles de l'utilisateur (claim standard "role") - on les relit tels quels
    // pour peupler le cookie du Web, afin que [Authorize(Roles = "Administrator")] fonctionne
    // côté Web sans dupliquer la logique d'autorisation déjà faite par les API.
    private static IEnumerable<Claim> GetRoleClaims(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return token.Claims
            .Where(c => c.Type is ClaimTypes.Role or "role")
            .Select(c => new Claim(ClaimTypes.Role, c.Value));
    }
}
