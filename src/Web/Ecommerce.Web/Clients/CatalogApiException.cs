namespace Ecommerce.Web.Clients;

// Levée quand Catalog.Api répond 400 (violation d'une règle métier, ex: prix négatif) — distingue
// ce cas d'une vraie panne réseau/service indisponible (HttpRequestException).
public sealed class CatalogApiException(string message) : Exception(message);
