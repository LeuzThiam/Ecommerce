namespace Ordering.Application;

// Représente l'appel HTTP vers Catalog.Api — l'implémentation (Infrastructure) utilisera un
// HttpClient, mais l'Application n'a besoin de connaître que ce contrat, pas les détails réseau.
public interface IProductCatalogClient
{
    Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}

// Un instantané minimal du produit au moment de la consultation — jamais l'entité Product de
// Catalog.Domain, qu'Ordering ne doit de toute façon pas connaître (ce sont deux services
// indépendants).
public sealed record ProductSnapshot(Guid Id, string Name, decimal Price, int Stock);
