namespace Ecommerce.Web.Clients;

// Levée quand Ordering.Api répond 400 (violation d'une règle métier, ex: stock insuffisant) —
// distingue ce cas d'une vraie panne réseau/service indisponible (HttpRequestException).
public sealed class OrderingApiException(string message) : Exception(message);
