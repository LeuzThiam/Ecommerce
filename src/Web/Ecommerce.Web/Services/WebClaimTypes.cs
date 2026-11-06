namespace Ecommerce.Web.Services;

// Nom de la claim stockant le JWT émis par Identity.Api dans le cookie d'authentification du
// Web, pour qu'un contrôleur [Authorize] puisse le reposer tel quel en en-tête Authorization
// vers les API en amont.
internal static class WebClaimTypes
{
    public const string AccessToken = "access_token";
}
