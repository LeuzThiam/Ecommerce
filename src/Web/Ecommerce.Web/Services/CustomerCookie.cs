namespace Ecommerce.Web.Services;

// Un visiteur non authentifié a besoin d'un identifiant stable pour son panier côté
// Ordering.Api. On le stocke dans un cookie (GUID), créé au premier accès. Pas de
// réconciliation avec un compte utilisateur réel pour l'instant — prévu avec le login (Issue #19).
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
}
