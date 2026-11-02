namespace Catalog.Application;

public interface IStockService
{
    Task<bool> TryReserveAsync(Guid productId, int quantity, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid productId, int quantity, CancellationToken cancellationToken);
}
