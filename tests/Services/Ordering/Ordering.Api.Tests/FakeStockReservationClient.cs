using Ordering.Application;

namespace Ordering.Api.Tests;

public sealed class FakeStockReservationClient : IStockReservationClient
{
    private readonly Dictionary<Guid, int> _stock = [];
    public List<(Guid ProductId, int Quantity)> ReleasedReservations { get; } = [];

    public void SetStock(Guid productId, int stock) => _stock[productId] = stock;

    public Task<bool> TryReserveAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var available = _stock.GetValueOrDefault(productId);
        if (available < quantity)
        {
            return Task.FromResult(false);
        }

        _stock[productId] = available - quantity;
        return Task.FromResult(true);
    }

    public Task ReleaseAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        _stock[productId] = _stock.GetValueOrDefault(productId) + quantity;
        ReleasedReservations.Add((productId, quantity));
        return Task.CompletedTask;
    }
}
