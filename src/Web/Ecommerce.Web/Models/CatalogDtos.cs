namespace Ecommerce.Web.Models;

// Ces types reproduisent volontairement la forme des réponses de Catalog.Api — le Web ne
// référence aucun projet Application/Domain des services backend (ce serait coupler deux
// services indépendants). Il consomme leur API REST comme n'importe quel client externe.
public sealed record ProductDto(Guid Id, string Name, string Slug, decimal Price, int Stock, Guid CategoryId, bool IsActive = true);

public sealed record CategoryDto(Guid Id, string Name);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
