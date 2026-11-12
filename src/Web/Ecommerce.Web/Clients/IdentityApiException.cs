namespace Ecommerce.Web.Clients;

// Même principe que OrderingApiException/CatalogApiException : Identity.Api répond 400
// (violation d'une règle métier, ex: se retirer soi-même les droits administrateur).
public sealed class IdentityApiException(string message) : Exception(message);
