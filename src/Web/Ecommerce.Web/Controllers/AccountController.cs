using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ecommerce.Web.Clients;
using Ecommerce.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public sealed class AccountController(IIdentityApiClient identityApiClient, IOrderingApiClient orderingApiClient) : Controller
{
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password, string displayName, CancellationToken cancellationToken)
    {
        var result = await identityApiClient.RegisterAsync(email, password, displayName, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View();
        }

        return RedirectToAction(nameof(RegisterConfirmation));
    }

    [HttpGet]
    public IActionResult RegisterConfirmation() => View();

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

        var userId = GetUserId(loginResult.AccessToken);

        // Avant de connecter l'utilisateur, on récupère son panier anonyme (cookie) s'il existe
        // et on le fusionne dans son panier de compte - sinon il perd tout ce qu'il avait ajouté
        // avant de se connecter (les deux paniers sont des entités Ordering.Api distinctes, tant
        // qu'on n'utilise pas le même Id pour les deux).
        var anonymousCustomerId = CustomerCookie.GetOrCreate(HttpContext);
        if (anonymousCustomerId != userId)
        {
            var anonymousCart = await orderingApiClient.GetCartAsync(anonymousCustomerId, cancellationToken);
            foreach (var item in anonymousCart.Items)
            {
                await orderingApiClient.AddItemAsync(userId, item.ProductId, item.Quantity, cancellationToken);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
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

        var orders = await orderingApiClient.GetOrderHistoryAsync(GetAccessToken(), cancellationToken);
        ViewBag.OrderCount = orders.Count;
        ViewBag.PendingCount = orders.Count(o => o.Status == "PendingPayment");
        ViewBag.PaidCount = orders.Count(o => o.Status == "Paid");
        ViewBag.RecentOrders = orders.OrderByDescending(o => o.CreatedAtUtc).Take(3).ToList();

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

    // Même lecture directe du JWT que GetRoleClaims : le "sub" du token est l'Id réel du compte
    // côté Identity.Api - c'est exactement celui qu'Ordering.Api utilise pour filtrer les
    // commandes (voir GetCustomerId côté Ordering.Api.Program.cs), donc le Web doit s'en servir
    // pour le panier/checkout plutôt que du GUID anonyme du cookie.
    private static Guid GetUserId(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var sub = token.Claims.First(c => c.Type is JwtRegisteredClaimNames.Sub or ClaimTypes.NameIdentifier).Value;
        return Guid.Parse(sub);
    }
}
