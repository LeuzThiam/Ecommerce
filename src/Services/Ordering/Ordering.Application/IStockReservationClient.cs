namespace Ordering.Application;

// Représente l'appel HTTP vers Catalog.Api (/api/catalog/stock/reserve et /release) — même
// principe que IProductCatalogClient : l'Application ne connaît que ce contrat.
public interface IStockReservationClient
{
    Task<bool> TryReserveAsync(Guid productId, int quantity, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid productId, int quantity, CancellationToken cancellationToken);
}
