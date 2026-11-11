using System.Security.Claims;

namespace Ecommerce.Web.Services;

// Un visiteur non authentifié a besoin d'un identifiant stable pour son panier côté
// Ordering.Api. On le stocke dans un cookie (GUID), créé au premier accès.
public static class CustomerCookie
{
    private const string CookieName = "ecommerce_customer_id";

    public static Guid GetOrCreate(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(CookieName, out var value) && Guid.TryParse(value, out var customerId))
        {
            return customerId;
        }

        customerId = Guid.NewGuid();
        httpContext.Response.Cookies.Append(CookieName, customerId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(365),
        });

        return customerId;
    }

    // Panier/checkout doivent utiliser le vrai Id du compte quand l'utilisateur est connecté -
    // sinon sa commande se retrouve associée au GUID anonyme du cookie, introuvable ensuite dans
    // "Mes commandes" (qui filtre par l'Id réel tiré du JWT). Le cookie anonyme ne sert donc plus
    // qu'aux visiteurs non connectés.
    public static Guid GetEffectiveCustomerId(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is not null && Guid.TryParse(userId, out var parsed) ? parsed : GetOrCreate(httpContext);
    }
}
