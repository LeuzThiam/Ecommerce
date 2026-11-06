namespace Ecommerce.Web.Models;

// Même principe que CatalogDtos.cs / OrderingDtos.cs : copie locale de la forme des réponses
// d'Identity.Api.
public sealed record UserDto(Guid Id, string Email, string DisplayName, DateTime CreatedAtUtc);

public sealed record AddressDto(Guid Id, string Line1, string? Line2, string City, string PostalCode, string Country, bool IsDefault);
